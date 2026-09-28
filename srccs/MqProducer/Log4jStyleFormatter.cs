using System;
using System.Text;

namespace MqProducer
{
    /// <summary>
    /// A log formatter that mimics the classic log4j layout:
    /// <code>
    ///   yyyy-MM-dd HH:mm:ss,SSS LEVEL [threadId] loggerName - message
    /// </code>
    /// The level field is right-padded to 5 characters (e.g. INFO , WARN ).
    /// The comma before milliseconds matches the log4j ISO8601 date pattern.
    /// </summary>
    public static class Log4jStyleFormatter
    {
        /// <summary>Formats a single log line.</summary>
        public static string Format(
            string    level,
            string    loggerName,
            string    message,
            int       threadId,
            DateTime  timestamp,
            Exception? exception = null)
        {
            string ts       = timestamp.ToString("yyyy-MM-dd HH:mm:ss,fff");
            string paddedLv = level.PadRight(5);

            var sb = new StringBuilder(128);
            sb.Append(ts)
              .Append(' ').Append(paddedLv)
              .Append(" [").Append(threadId).Append(']')
              .Append(' ').Append(loggerName)
              .Append(" - ").Append(message)
              .Append(Environment.NewLine);

            if (exception != null)
            {
                sb.Append(exception.ToString()).Append(Environment.NewLine);
            }

            return sb.ToString();
        }
    }
}
