using System;

namespace Salient.JsonClient.Tests
{
    public class DummyJsonExceptionFactory : IJsonExceptionFactory
    {
        public Exception ParseException(string json)
        {
            if (json.Contains("error"))
            {
                return new Exception("there was an error");
            }
            return null;
        }

        public Exception ParseException(string extraInfo, string json, Exception inner)
        {
            if (json.Contains("error"))
            {
                return new Exception("there was an error", inner);
            }
            return null;
        }
    }
}