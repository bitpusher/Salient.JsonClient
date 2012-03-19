using System;
using System.Text;

namespace Salient.ReliableHttpClient
{
    /// <summary>
    /// NOTE: this class must be kept serializable by not populating inner exception and keeping all
    /// properties serializable
    /// </summary>
    public class ReliableHttpException : Exception
    {
        public static ReliableHttpException Create(Exception exception)
        {
       
            if (exception is ReliableHttpException)
            {
                return new ReliableHttpException((ReliableHttpException)exception);
            }
            return new ReliableHttpException(exception);
        }

        public string InnerExceptionType { get; set; }
        public string InnerStackTrace { get; set; }
        public ReliableHttpException(Exception exception)
            : base(exception.Message)
        {
            InnerExceptionType = exception.GetType().FullName;
            InnerStackTrace = exception.StackTrace;
        }

        /// <summary>
        /// basically a clone constructor
        /// </summary>
        /// <param name="exception"></param>
        public ReliableHttpException(ReliableHttpException exception)
            : base(exception.Message)
        {
            InnerExceptionType = exception.InnerExceptionType;
            InnerStackTrace = exception.InnerStackTrace;

        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Message);
            sb.AppendLine(InnerExceptionType);
            sb.AppendLine(InnerStackTrace);
            return sb.ToString();
        }
    }
}