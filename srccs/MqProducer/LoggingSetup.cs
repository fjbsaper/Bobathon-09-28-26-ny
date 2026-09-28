using System;
using System.IO;
using System.Threading;

namespace MqProducer
{
    /// <summary>
    /// Thread-safe file logger that writes in log4j style via <see cref="Log4jStyleFormatter"/>.
    /// Call <see cref="Init"/> once from <c>Program</c> before any worker threads start.
    /// Obtain a per-class logger via <see cref="GetLogger"/>.
    /// </summary>
    public sealed class AppLogger
    {
        private static StreamWriter? _writer;
        private static readonly object _lock = new();

        private readonly string _name;

        private AppLogger(string name) => _name = name;

        // -----------------------------------------------------------------------
        // Static API
        // -----------------------------------------------------------------------

        /// <summary>
        /// Initialises logging to <paramref name="logFile"/> in append mode.
        /// Must be called exactly once before any worker threads are started.
        /// </summary>
        public static void Init(string logFile)
        {
            _writer = new StreamWriter(logFile, append: true) { AutoFlush = true };
        }

        /// <summary>Returns a named logger for the given class/component.</summary>
        public static AppLogger GetLogger(string name) => new AppLogger(name);

        // -----------------------------------------------------------------------
        // Instance API
        // -----------------------------------------------------------------------

        public void Info(string message)  => Write("INFO",  message);
        public void Warn(string message)  => Write("WARN",  message);
        public void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private void Write(string level, string message, Exception? ex = null)
        {
            int      tid  = Thread.CurrentThread.ManagedThreadId;
            DateTime now  = DateTime.Now;
            string   line = Log4jStyleFormatter.Format(level, _name, message, tid, now, ex);

            lock (_lock)
            {
                _writer?.Write(line);
            }
        }
    }
}
