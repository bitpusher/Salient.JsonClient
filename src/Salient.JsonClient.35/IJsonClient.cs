﻿using System;
using System.Collections.Generic;

namespace Salient.JsonClient
{
    ///<summary>
    /// Converts json text to service specific exception
    ///</summary>
    public interface IJsonExceptionFactory
    {
        ///<summary>
        ///</summary>
        ///<param name="json"></param>
        ///<returns>Null if json is not recognized as an exception</returns>
        Exception ParseException(string json);

        Exception ParseException(string extraInfo, string json, Exception inner);
    }


    ///<summary>
    /// Returns null in all cases
    ///</summary>
    public class NullJsonExceptionFactory : IJsonExceptionFactory
    {
        public Exception ParseException(string json)
        {
            return null;
        }

        public Exception ParseException(string extraInfo, string json, Exception inner)
        {
            return null;
        }


    }
    ///<summary>
    /// Describes a general purpose HTTP JSON API client
    ///</summary>
    public interface IJsonClient : IDisposable
    {

        ///<summary>
        ///</summary>
        IRequestController RequestController { get; }

        /// <summary>
        /// Very simple synchronous wrapper of the begin/end methods.
        /// I have chosen not to simply use the synchronous .GetResponse() method of WebRequest to prevent evolution
        /// of code that will not port to silverlight. While it is against everything righteous and holy in the silverlight crowd
        /// to implement syncronous patterns, no matter how cleverly, there is just too much that can be done with a sync fetch, i.e. multi page, eager fetches, etc,
        /// to ignore it. We simply forbid usage on the UI thread with an exception. Simple.
        /// </summary>
        /// <typeparam name="TDTO"></typeparam>
        /// <param name="target"></param>
        /// <param name="uriTemplate"></param>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        /// <param name="cacheDuration"></param>
        /// <param name="throttleScope"></param>
        /// <returns></returns>
        TDTO Request<TDTO>(string target, string uriTemplate, string method, Dictionary<string, object> parameters, TimeSpan cacheDuration, string throttleScope);
        /// <summary>
        /// Standard async Begin implementation.
        /// </summary>
        /// <typeparam name="TDTO"></typeparam>
        /// <param name="cb"></param>
        /// <param name="state"></param>
        /// <param name="target"></param>
        /// <param name="uriTemplate"></param>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        /// <param name="cacheDuration"></param>
        /// <param name="throttleScope"></param>
        void BeginRequest<TDTO>(ApiAsyncCallback<TDTO> cb, object state, string target, string uriTemplate, string method, Dictionary<string, object> parameters, TimeSpan cacheDuration, string throttleScope);
        /// <summary>
        /// Standard async end implementation. Calling code passes in the ApiAsyncResult that is returned to the callback
        /// and the response data is returned. If an exception occurred during execution of the request, it will now be
        /// rethrown.
        /// </summary>
        /// <typeparam name="TDTO"></typeparam>
        /// <param name="asyncResult"></param>
        /// <returns></returns>
        /// <exception cref="ApiException">the exception, if any, that occurred during execution of the request</exception>
        TDTO EndRequest<TDTO>(ApiAsyncResult<TDTO> asyncResult);
    }
}