using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


using NUnit.Framework;

namespace Salient.JsonClient.Tests
{
    [TestFixture]
    public class CacheFixture : LoggingFixtureBase
    {
        [Test]
        public void ItemCanBeCached()
        {
            var c = new RequestCache(TimeSpan.MinValue);
            lock (c)
            {
                var item = c.GetOrCreate<FooDTO>("foo");
                item.Expiration = DateTimeOffset.UtcNow.AddSeconds(2);
                item.ItemState = CacheItemState.Complete;

                new AutoResetEvent(false).WaitOne(1000);
                var actual = c.Get<FooDTO>("foo");
                Assert.IsNotNull(actual);
            }
            
        }

        [Test, ExpectedException(ExpectedMessage = "item for foo was not found in the cache")]
        public void ItemCanExpireAndBePurged()
        {
            var c = new RequestCache(TimeSpan.MinValue);
            lock (c)
            {
                var item = c.GetOrCreate<FooDTO>("foo");
                item.Expiration = DateTimeOffset.UtcNow.AddSeconds(1);
                item.ItemState = CacheItemState.Complete;

                new AutoResetEvent(false).WaitOne(2000);
                c.PurgeExpiredItems(null);
                new AutoResetEvent(false).WaitOne(1000);
                c.Get<FooDTO>("foo");
                Assert.Fail("Expected exception");
            }
        }

    }
}
