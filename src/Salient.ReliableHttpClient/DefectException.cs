using System;

namespace Salient.ReliableHttpClient
{
    /// <summary>
    /// throw this exception when something has obviously gone wrong with the
    /// program logic.
    /// </summary>
    public class DefectException : Exception
    {
        public DefectException(string message)
            : base(message)
        {

        }

    }
}