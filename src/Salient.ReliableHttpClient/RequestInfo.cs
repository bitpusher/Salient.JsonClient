﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using Salient.ReflectiveLoggingAdapter;

namespace Salient.ReliableHttpClient
{
    public class RequestInfo : RequestInfoBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (RequestInfo));

        private RequestInfo()
        {
            Callbacks = new Queue<CallbackInfo>();
        }


        public Queue<CallbackInfo> Callbacks { get; private set; }
        public WebRequest Request { get; private set; }

        public AutoResetEvent ProcessingWaitHandle { get; private set; }
        internal event EventHandler ProcessingComplete;


        internal void BuildRequest(IRequestFactory requestFactory)
        {
            Request = CreateRequest(this, requestFactory);
        }


        public static RequestInfo Create(
            RequestMethod method, string target, string uriTemplate, Dictionary<string, object> parameters,
            string userAgent, Dictionary<string, object> headers, ContentType requestContentType,
            ContentType responseContentType, TimeSpan cacheDuration,
            int timeout, int retryCount,
            Uri uri, string requestBody, IRequestFactory requestFactory)
        {
            var result = new RequestInfo
                             {
                                 Target = target,
                                 UriTemplate = uriTemplate,
                                 AllowedRetries = retryCount,
                                 Uri = uri,
                                 Method = method,
                                 UserAgent = userAgent,
                                 Headers = headers,
                                 RequestBody = requestBody,
                                 Parameters = parameters,
                                 CacheDuration = cacheDuration,
                                 RequestContentType = requestContentType,
                                 ResponseContentType = responseContentType,
                                 Timeout = timeout
                             };

            return result;
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void CompleteRequest(IAsyncResult result)
        {
            // NOTE: the ONLY time an exception should leak from this method is when a request 
            // wishes to be retried.

            State = RequestItemState.Processing;
            
            WebResponse response = null;


            try
            {
                using (response = Request.EndGetResponse(result))
                using (Stream stream = response.GetResponseStream())
                    // ReSharper disable AssignNullToNotNullAttribute
                using (var reader = new StreamReader(stream))
                    // ReSharper restore AssignNullToNotNullAttribute
                {
                    string json = reader.ReadToEnd();
                    Completed = DateTimeOffset.UtcNow;
                    ResponseText = json;
                    Log.Debug(string.Format("request completed: latency {1}\r\nITEM\r\n{0}", this,
                                            Completed.Subtract(Issued).Duration()));
                }
            }
            catch (Exception ex)
            {
                Exception = ReliableHttpException.Create(ex);
                // if we have allowed retries, log the error and throw it so the controller can retry for us
                if (AttemptedRetries < AllowedRetries)
                {
                    throw;
                }
                // otherwise just compelte the callbacks with the error
            }

            if (Exception != null)
            {
                Log.Error(string.Format("request failed {0}: attempts {1} : error:{2}", Id, AttemptedRetries,
                                        Exception.Message));
            }
            while (Callbacks.Count > 0)
            {
                CallbackInfo callback = Callbacks.Dequeue();

                // since the request is complete, this callback will be called immediately
                try
                {
                    new ReliableAsyncResult(callback.Callback, callback.State, true, ResponseText, Exception);
                }
                catch (Exception ex)
                {
                    // there is nothing for us to do here but log the unhandled exception coming out of the consuming code.
                    // not our fault and not our problem. at this point it goes into the ether.

                    string errorMessage = "Error processing callback: " + ex.Message;
                    Log.Error(errorMessage, ex);
                }
            }

            CacheExpiration = DateTimeOffset.UtcNow.Add(CacheDuration);
            State = RequestItemState.Complete;
            try
            {
                OnProcessingComplete();
            }
            catch (Exception ex)
            {
                // there is nothing for us to do here but log the unhandled exception coming out of the consuming code.
                // not our fault and not our problem. at this point it goes into the ether.

                string errorMessage = "Error processing event handler: " + ex.Message;
                Log.Error(errorMessage, ex);
            }
        }


        private void OnProcessingComplete()
        {
            EventHandler handler = ProcessingComplete;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public void AddCallback(ApiAsyncCallback callback, object state)
        {
            lock (Callbacks)
            {
                Callbacks.Enqueue(new CallbackInfo {Callback = callback, State = state});
            }
        }
    }
}