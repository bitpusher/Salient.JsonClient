﻿using System;
using System.IO;
using System.Net;

namespace Salient.JsonClient
{
    /// <summary>
    /// An exception class that will retrieve the response text of the inner exception if it is a WebException
    /// </summary>
    public class ApiException : Exception
    {
        public override string ToString()
        {
            return base.ToString() + "\r\nException ResponseText:\r\n" + ResponseText;
        }

        internal ApiException(Exception inner)
            : base(inner.Message, inner)
        {
            if (inner is WebException)
            {
                ResponseText = GetResponseText((WebException)inner);
            }
        }

        ///<summary>
        ///</summary>
        ///<param name="message"></param>
        public ApiException(string message)
            : base(message)
        {
        }


        ///<summary>
        ///</summary>
        ///<param name="message"></param>
        ///<param name="inner"></param>
        public ApiException(string message, Exception inner)
            : base(message, inner)
        {

            try
            {

                if (inner is ApiException)
                {
                    ResponseText = ((ApiException)inner).ResponseText;
                }
            }
            // ReSharper disable EmptyGeneralCatchClause

            catch
            // ReSharper restore EmptyGeneralCatchClause
            {


            }
        }


        ///<summary>
        ///</summary>
        public string ResponseText { get; set; }

        ///<summary>
        ///</summary>
        ///<param name="inner"></param>
        ///<returns></returns>
        public static ApiException Create(Exception inner)
        {
            if (inner is ApiException)
            {
                return (ApiException)inner;
            }

            return new ApiException(inner);
        }

        public static string GetResponseText(WebException exception)
        {
            string json = null;
           
            try
            {

                if (exception.Response != null)
                {
                    using (Stream stream = exception.Response.GetResponseStream())
                    {
                        if (stream != null)
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                json = reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch
            {
                // should swallow?
                json = "Could not parse exception response";
            }

           
       
            return json;
        }
    }

    ///<summary>
    ///</summary>
    public class ApiTimeoutException :ApiException
    {
        internal ApiTimeoutException(Exception inner) : base(inner)
        {
        }

        public ApiTimeoutException(string message) : base(message)
        {
        }

        public ApiTimeoutException(string message, Exception inner) : base(message, inner)
        {
        }
    }
    ///<summary>
    ///</summary>
    public class ApiSerializationException : ApiException
    {
        ///<summary>
        ///</summary>
        ///<param name="message"></param>
        ///<param name="responseText"></param>
        public ApiSerializationException(string message, string responseText)
            : base(message)
        {
            ResponseText = responseText;
        }
        ///<summary>
        ///</summary>
        ///<param name="message"></param>
        ///<param name="responseText"></param>
        ///<param name="inner"></param>
        public ApiSerializationException(string message, string responseText,Exception inner)
            : base(message, inner)
        {
            ResponseText = responseText;
        }
    }


    ///<summary>
    ///</summary>
    public class ResponseHandlerException : ApiException
    {
        internal ResponseHandlerException(Exception inner)
            : base(inner)
        {
            if (inner is ApiException)
            {
                ResponseText = ((ApiException)inner).ResponseText;
            }
        }

        internal ResponseHandlerException(string message)
            : base(message)
        {
        }

        internal ResponseHandlerException(string message, Exception inner)
            : base(message, inner)
        {
            if (inner is ApiException)
            {
                ResponseText = ((ApiException)inner).ResponseText;
            }
        }
    }
}