using System;
using System.IO;
using System.Text;

namespace MqConsumer
{
    /// <summary>
    /// A log formatter that mimics the classic log4j layout:
    /// <code>
    ///   yyyy-MM-dd HH:mm:ss,SSS LEVEL [threadId] loggerName - message
    /// </code>
    /// The level field is right-padded to 5 characters (e.g. INFO , WARN ).
    /// The comma before milliseconds matches the log4j ISO8601 date pattern.
    /// Thread ID is the managed thread ID from <see cref="System.Threading.Thread.CurrentThread"/>.
    /// </summary>
    public static class Log4jStyleFormatter
    {
        /// <summary>
        /// Formats a single log line.
        /// </summary>
        /// <param name="level">Log level label (e.g. "INFO", "WARN", "ERROR").</param>
        /// <param name="loggerName">Logger / class name.</param>
        /// <param name="message">Message text.</param>
        /// <param name="threadId">Managed thread ID.</param>
        /// <param name="timestamp">Timestamp for the record.</param>
        /// <param name="exception">Optional exception; its stack trace is appended when non-null.</param>
        /// <returns>Formatted log line(s) ending with a newline.</returns>
        public static string Format(
            string    level,
            string    loggerName,
            string    message,
            int       threadId,
            DateTime  timestamp,
            Exception? exception = null)
        {
            // log4j ISO8601: yyyy-MM-dd HH:mm:ss,SSS  (comma before ms)
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
