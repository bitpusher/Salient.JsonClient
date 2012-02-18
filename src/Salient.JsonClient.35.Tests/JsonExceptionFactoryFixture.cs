using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Salient.JsonClient.Tests
{
    [TestFixture]
    public class JsonExceptionFactoryFixture : LoggingFixtureBase
    {

        [Test, ExpectedException(typeof(ApiException), ExpectedMessage = "there was an error")]
        public void ErrorJsonIsRecognizedAndThrown()
        {


            var ctx = BuildClientAndSetupResponse("{\"error\":\"foo\"}");

            ctx.Request<TestDto>("foo", "GET");

        }


        private Client BuildClientAndSetupResponse(string expectedJson)
        {

            TestRequestFactory factory = new TestRequestFactory();
            var requestController = new RequestController(TimeSpan.FromSeconds(0), 2, factory, new DummyJsonExceptionFactory(),  new ThrottledRequestQueue(TimeSpan.FromSeconds(5), 30, 10, "default"));

            var ctx = new Client(new Uri("http://foo.bar"), requestController);
            factory.CreateTestRequest(expectedJson);
            return ctx;
        }
    }
}
