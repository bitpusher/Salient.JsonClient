﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using NUnit.Framework;

namespace Salient.JsonClient.Tests
{
    [TestFixture]
    public class CacheItemFixture
    {
        public class TestDTO { }

        [Test]
        public void CacheItemToString()
        {
            var item = new CacheItem<TestDTO>
                           {
                               CacheDuration = TimeSpan.FromSeconds(1),
                               Exception = new Exception("exception message"),
                               ItemState = CacheItemState.New,
                               Method = "GET",
                               Parameters = new Dictionary<string, object> {{"key", "value"}},
                               ResponseText = "ResponseText",
                               RetryCount = 3,
                               Target = "target",
                               ThrottleScope = "throttleScope",
                               UriTemplate = "uriTemplate",
                               Url = "url",
                               Request = WebRequest.Create("http://tempuri.org")
                           };

            item.Request.Headers = new WebHeaderCollection() { { "name", "value" } };

            var actual = item.ToString();

            const string expected = @"ItemState       : New
Url             : url
Method          : GET
Target          : target
UriTemplate     : uriTemplate
Parameters      : 
	key: ""value""
Request URI     : http://tempuri.org/
Request Headers : 
	name: value
CacheDuration   : 00:00:01
RetryCount      : 3
ThrottleScope   : throttleScope
Expiration      : 0001-01-01 00:00:00Z
ResponseText    : ResponseText
Exception       : System.Exception: exception message
";

            Assert.AreEqual(expected,actual);


        }

        [Test]
        public void CacheItemToStringNoException()
        {
            var item = new CacheItem<TestDTO>
                           {
                               CacheDuration = TimeSpan.FromSeconds(1),
                               ItemState = CacheItemState.New,
                               Method = "GET",
                               Parameters = new Dictionary<string, object> {{"key", "value"}},
                               ResponseText = "ResponseText",
                               RetryCount = 3,
                               Target = "target",
                               ThrottleScope = "throttleScope",
                               UriTemplate = "uriTemplate",
                               Url = "url",
                               Request = WebRequest.Create("http://tempuri.org")
                           };

            item.Request.Headers = new WebHeaderCollection() { { "name", "value" } };


            var actual = item.ToString();


            const string expected = @"ItemState       : New
Url             : url
Method          : GET
Target          : target
UriTemplate     : uriTemplate
Parameters      : 
	key: ""value""
Request URI     : http://tempuri.org/
Request Headers : 
	name: value
CacheDuration   : 00:00:01
RetryCount      : 3
ThrottleScope   : throttleScope
Expiration      : 0001-01-01 00:00:00Z
ResponseText    : ResponseText
";

            Assert.AreEqual(expected, actual);


        }

        [Test]
        public void CacheItemToStringNoExceptionNoRequest()
        {
            var item = new CacheItem<TestDTO>
                           {
                               CacheDuration = TimeSpan.FromSeconds(1),
                               ItemState = CacheItemState.New,
                               Method = "GET",
                               Parameters = new Dictionary<string, object> {{"key", "value"}},
                               ResponseText = "ResponseText",
                               RetryCount = 3,
                               Target = "target",
                               ThrottleScope = "throttleScope",
                               UriTemplate = "uriTemplate",
                               Url = "url"
                           };

            var actual = item.ToString();

            const string expected = @"ItemState       : New
Url             : url
Method          : GET
Target          : target
UriTemplate     : uriTemplate
Parameters      : 
	key: ""value""
CacheDuration   : 00:00:01
RetryCount      : 3
ThrottleScope   : throttleScope
Expiration      : 0001-01-01 00:00:00Z
ResponseText    : ResponseText
";

            Assert.AreEqual(expected, actual);


        }
    }
}
