using NUnit.Framework;
using Salient.ReflectiveLoggingAdapter;

namespace Salient.JsonClient.Tests
{
    public class LoggingFixtureBase
    {
        static LoggingFixtureBase()
        {

            //Hook up a logger for the CIAPI.CS libraries
            LogManager.CreateInnerLogger = (logName, logLevel, showLevel, showDateTime, showLogName, dateTimeFormat)
                                           => new SimpleDebugAppender(logName, logLevel, showLevel, showDateTime, showLogName, dateTimeFormat);
        }
    }

    [TestFixture]
    public class LoggingFixture : LoggingFixtureBase
    {
        [Test]
        public void CheckForSymbols()
        {
            var logger = LogManager.GetLogger(this.GetType());
            logger.Debug("foo");
            
        }
    }
}