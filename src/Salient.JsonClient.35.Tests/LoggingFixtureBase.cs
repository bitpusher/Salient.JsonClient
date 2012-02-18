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
}