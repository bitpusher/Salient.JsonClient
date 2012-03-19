using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;

using System.Threading;
using Salient.ReflectiveLoggingAdapter;

namespace Salient.ReliableHttpClient
{
    /// <summary>
    /// RequestController will encapsulate threadsafe caching and throttling
    /// items with a cacheduration of 0 will not be cached
    /// items flagged as unique
    /// </summary>
    public class RequestController : IDisposable
    {
        public Recorder Recorder { get; set; }
        private static readonly ILog Log = LogManager.GetLogger(typeof(RequestController));
        private const int BackgroundInterval = 50;
        private readonly int _maxPendingRequests = 10;
        private readonly int _throttleWindowCount = 30;
        private readonly TimeSpan _throttleWindowTime = TimeSpan.FromSeconds(10);

        private readonly Thread _backgroundThread;
        private bool _processingQueue;
        private int _dispatchedCount;
        private readonly AutoResetEvent _waitHandle;

        private readonly Queue<DateTimeOffset> _requestTimes = new Queue<DateTimeOffset>();
        private readonly Queue<RequestInfo> _requests = new Queue<RequestInfo>();

        private bool _notifiedWaitingOnMaxPending;
        private bool _notifiedWaitingOnWindow;
        private int _outstandingRequests;


        private bool _disposed;
        private volatile bool _disposing;

        public string UserAgent { get; set; }
        private readonly List<RequestInfo> _requestCache;
        private IRequestFactory _requestFactory;
        private Queue<RequestInfo> _requestQueue;
        public Guid Id { get; private set; }
        public RequestController(IRequestFactory requestFactory)
            : this()
        {
            _requestFactory = requestFactory;
        }
        public RequestController()
        {
            Recorder = new Recorder { Paused = true };
            Id = Guid.NewGuid();
            Log.Debug("creating RequestController: " + Id);
            _requestFactory = new RequestFactory();
            _requestCache = new List<RequestInfo>();
            _requestQueue = new Queue<RequestInfo>();
            _waitHandle = new AutoResetEvent(false);
            _backgroundThread = new Thread(BackgroundProcess);
            _backgroundThread.Start();
            Log.Debug("created RequestController: " + Id);
        }

        private void BackgroundProcess(object ignored)
        {
            Log.Debug("RequestController  " + Id + " background process created");

            while (true)
            {
                //lock (_lock)
                {
                    // passive shut down of thread to avoid spurious ThreadAbortException from
                    // popping up in arbitrary places as is wont to happen when just killing a thread.
                    if (_disposing)
                    {
                        Log.Debug("RequestController  " + Id + " shutting down");
                        return;
                    }

                    // TODO: how/why/should we surface exceptions?
                    // Any exceptions leaking from this point should be critical
                    // unhandled exceptions. Most exceptions will just be passed to the
                    // async completion callbacks.

                    PurgeExpiredItems();


                    ProcessQueue();
                }
                _waitHandle.WaitOne(BackgroundInterval);
            }

        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ProcessQueue()
        {

            //lock (_lockTarget)
            {

                if (_processingQueue) return;
                // should i be locking on the queue? (NOTE: concurrent libs cannot be used due to SL platform)
                if (_requestQueue.Count == 0) return;

                _processingQueue = true;


                RequestInfo request = _requestQueue.Peek();



                try
                {
                    if (ThereAreMoreOutstandingRequestsThanIsAllowed()) return;

                    if (_requestTimes.Count > _throttleWindowCount)
                    {
                        throw new Exception("request time queue got to be longer than window somehow");
                    }

                    if (_requestTimes.Count == _throttleWindowCount)
                    {
                        DateTimeOffset head = _requestTimes.Peek();
                        TimeSpan waitTime = (_throttleWindowTime - (DateTimeOffset.UtcNow - head));

                        if (waitTime.TotalMilliseconds > 0)
                        {
                            if (!_notifiedWaitingOnWindow)
                            {
                                string msgWaiting = string.Format("Waiting: " + waitTime + " to send " + request.Uri);
                                Log.Info(msgWaiting);

                                _notifiedWaitingOnWindow = true;
                            }
                            return;
                        }
                        _requestTimes.Dequeue();
                    }


                    // good to go. 
                    _notifiedWaitingOnWindow = false;

                    _requestTimes.Enqueue(DateTimeOffset.UtcNow);
                    _dispatchedCount += 1;

                    request.Index = _dispatchedCount;

                    try
                    {
                        request.Issued = DateTimeOffset.UtcNow;
                        var webRequestAsyncResult = request.Request.BeginGetResponse(ar =>
                        {
                            Log.Info(string.Format("Received #{0} : {1} ", request.Index, request.Uri));

                            // let's try to complete the request
                            try
                            {
                                request.CompleteRequest(ar);

                            }
                            catch (Exception ex)
                            {
                                // the only time an exception will come out of CompleteRequest is if the request wants to be retried
                                request.AttemptedRetries++;
                                Log.Warn(string.Format("retrying request {0}: attempt #{1} : error:{2}", request.Id, request.AttemptedRetries, ex.Message));
                                // create a new httprequest for this guy
                                request.BuildRequest(_requestFactory);
                                //put it back in the queue. if it belongs in the cache it is already there.
                                _requestQueue.Enqueue(request);
                            }


                        }, null);


                        EnsureRequestWillAbortAfterTimeout(request, webRequestAsyncResult);

                        Log.Info(string.Format("Dispatched #{0} : {1} ", request.Index, request.Uri));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Format("Error dispatching #{0} : {1} \r\n{2}", request.Index, request.Uri, ex.Message));

                        throw;
                    }
                    finally
                    {
                        _requestQueue.Dequeue();
                        _outstandingRequests++; // TODO: should this really be here if there was an error that prevented dispatch?
                    }

                }
                finally
                {
                    _processingQueue = false;
                }
                //Log.DebugFormat("exiting lock for ProcessQueue()");
            }
            //Log.DebugFormat("outside lock for ProcessQueue()");
        }

        private bool ThereAreMoreOutstandingRequestsThanIsAllowed()
        {
            if (_outstandingRequests > _maxPendingRequests)
            {
                if (!_notifiedWaitingOnMaxPending)
                {
                    string msgMaxPending = string.Format("Waiting: pending requests {0}", _outstandingRequests);
                    Log.Info(msgMaxPending);

                    _notifiedWaitingOnMaxPending = true;
                }

                return true;
            }

            _notifiedWaitingOnMaxPending = false;
            return false;
        }

        private static void EnsureRequestWillAbortAfterTimeout(RequestInfo request, IAsyncResult result)
        {
            //TODO: How can we timeout a request for Silverlight, when calls to AsyncWaitHandle throw the following:
            //   Specified method is not supported. at System.Net.Browser.OHWRAsyncResult.get_AsyncWaitHandle() 

            // DAVID: i don't think that the async methods have a timeout parameter. we will need to build one into 
            // it. will not be terribly clean as it will prolly have to span both the throttle and the cache. I will look into it


#if !SILVERLIGHT
            ThreadPool.RegisterWaitForSingleObject(
                    waitObject: result.AsyncWaitHandle,
                    callBack: (state, isTimedOut) =>
                    {
                        if (!isTimedOut) return;
                        if (state.GetType() != typeof(RequestInfo)) return;

                        var rh = (RequestInfo)state;
                        Log.Error(string.Format("Aborting #{0} : {1} because it has exceeded timeout {2}", rh.Index, rh.Request.RequestUri, rh.Request.Timeout));
                        rh.Request.Abort();
                    },
                    state: request,
                    timeout: TimeSpan.FromMilliseconds(request.Request.Timeout),
                    executeOnlyOnce: true);
#endif
        }

        private void PurgeExpiredItems()
        {
            lock (_requestCache)
            {
                var toRemove = new List<RequestInfo>();

                // ReSharper disable LoopCanBeConvertedToQuery
                foreach (RequestInfo item in _requestCache)
                // ReSharper restore LoopCanBeConvertedToQuery
                {
                    if (item.CacheExpiration <= DateTimeOffset.UtcNow &&
                        item.State == RequestItemState.Complete)
                    {
                        toRemove.Add(item);
                    }
                }

                foreach (var item in toRemove)
                {
                    try
                    {
                        _requestCache.Remove(item);
                        Log.Info(string.Format("Removed {0} from cache", item.Uri));
                    }
                    catch (Exception ex)
                    {
                        Log.Warn(string.Format("Unable to remove item {0} from cache", item.Uri), ex);
                        // swallow
                    }

                }
            }
        }

        private RequestInfo GetRequestInfo(Uri uri)
        {
            RequestInfo info = _requestCache.FirstOrDefault(r => r.Uri.AbsoluteUri == uri.AbsoluteUri);
            if (info != null)
            {
                switch (info.State)
                {
                    case RequestItemState.Processing:
                        // processing of this item's callbacks has already commenced. it is not viable
                        return null;
                    case RequestItemState.New:
                    case RequestItemState.Preparing:
                        throw new DefectException(
                            "this class is supposed to be threadsafe, an item should never be available as New or Preparing");

                    case RequestItemState.Ready:
                    case RequestItemState.Pending:
                    case RequestItemState.Complete:
                        // this item is eligible for adding a callback
                        return info;
                    default:
                        throw new DefectException(
                            "a field has been added to the RequestItemState enum that we are not handling.");
                }
            }
            return null;
        }




        private RequestInfo CreateRequest(Uri uri, RequestMethod method, string body, Dictionary<string, object> headers, ContentType requestContentType, ContentType responseContentType, TimeSpan cacheDuration, int timeout, string target, string uriTemplate, int retryCount, Dictionary<string, object> parameters, ApiAsyncCallback callback, object state)
        {
            RequestInfo info = RequestInfo.Create(method, target, uriTemplate, parameters, UserAgent, headers, requestContentType, responseContentType, cacheDuration, timeout, retryCount, uri, body, _requestFactory);
            info.BuildRequest(_requestFactory);
            info.ProcessingComplete += RequestCompleted;
            if (callback != null)
            {
                info.AddCallback(callback, state);
            }
            return info;
        }

        void RequestCompleted(object sender, EventArgs e)
        {
            var info = (RequestInfo)sender;
            info.ProcessingComplete -= RequestCompleted;
            RequestInfoBase copy = info.Copy();
            Recorder.AddRequest(copy);
        }


        public Guid BeginRequest(Uri uri, RequestMethod method, string body, Dictionary<string, object> headers, ContentType requestContentType, ContentType responseContentType, TimeSpan cacheDuration, int timeout, string target, string uriTemplate, int retryCount, Dictionary<string, object> parameters, ApiAsyncCallback callback, object state)
        {
            lock (_requestCache)
            {
                RequestInfo info = null;

                if (cacheDuration == TimeSpan.Zero || (method == RequestMethod.PUT) || (method == RequestMethod.POST))
                {
                    // this item should not enter the cache
                    info = CreateRequest(uri, method, body, headers, requestContentType, responseContentType, cacheDuration, timeout, target, uriTemplate, retryCount, parameters, callback, state);

                    _requestQueue.Enqueue(info);

                }
                else
                {
                    // this item can enter the cache
                    if (!string.IsNullOrEmpty(body))
                    {
                        throw new Exception("a request with body cannot be cached. body is supported on PUT and POST only");
                    }
                    info = GetRequestInfo(uri);
                    if (info == null)
                    {

                        info = CreateRequest(uri, method, body, headers, requestContentType, responseContentType, cacheDuration, timeout, target, uriTemplate, retryCount, parameters, callback, state);

                        _requestCache.Add(info);
                        _requestQueue.Enqueue(info);
                        Log.Info(string.Format("Added {0} to cache", info.Uri));
                    }
                    info.AddCallback(callback, state);
                }
                return info.Id;
            }

        }

        public void EndRequest(ReliableAsyncResult result)
        {
            lock (_requestCache)
            {

                try
                {
                    result.End();
                }
                catch (Exception)
                {

                    throw;
                }



            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _disposing = true;
                    while (_backgroundThread.IsAlive)
                    {
                        Thread.Sleep(100);
                    }
                }

                _disposed = true;
            }
        }
    }
}