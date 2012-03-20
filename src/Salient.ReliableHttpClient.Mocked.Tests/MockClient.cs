﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Salient.ReliableHttpClient;

using Salient.ReliableHttpClient.Serialization;
using Salient.ReliableHttpClient.Testing;

namespace Salient.ReliableHttpClient.Mocked.Tests
{
    public class MockClient : ClientBase
    {
        public MockClient(IJsonSerializer serializer)
            : base(serializer)
        {
            TestRequestFactory requestFactory = new TestRequestFactory();
            Controller = new RequestController(requestFactory);
        }
    }
}
