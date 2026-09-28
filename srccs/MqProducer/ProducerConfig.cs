using System;
using System.Collections.Generic;
using System.IO;

namespace MqProducer
{
    /// <summary>
    /// Loads and validates producer.properties.
    /// All runtime configuration is accessed through typed properties on this class;
    /// no other class opens files or parses strings directly.
    /// </summary>
    public sealed class ProducerConfig
    {
        public string CcdtUrl           { get; }
        public string QueueManager      { get; }
        public string Queue             { get; }
        public int    ThreadCount       { get; }
        public int    MessagesPerThread { get; }
        public string LogFile           { get; }
        /// <summary>
        /// Number of reconnect attempts the XMS client makes before reporting a connection failure.
        /// Maps to <c>XMSC.WMQ_CLIENT_RECONNECT_TIMEOUT</c> (seconds).
        /// Set to -1 to retry indefinitely.
        /// </summary>
        public int    ReconnectCount    { get; }
        /// <summary>
        /// Delay in seconds between reconnect attempts.
        /// Stored here for informational logging.
        /// </summary>
        public int    ReconnectDelay    { get; }
        /// <summary>
        /// CCSID used for message encoding. 1208 = UTF-8.
        /// Maps to <c>XMSC.WMQ_CCSID</c>.
        /// </summary>
        public int    CcsId             { get; }

        /// <param name="propertiesPath">Path to the producer.properties file.</param>
        /// <exception cref="IOException">If the file cannot be read.</exception>
        /// <exception cref="ArgumentException">If a required key is missing or has an invalid value.</exception>
        public ProducerConfig(string propertiesPath)
        {
            var props = LoadProperties(propertiesPath);

            CcdtUrl           = Require(props, "mq.ccdtUrl");
            QueueManager      = Require(props, "mq.queueManager");
            Queue             = Require(props, "mq.queue");
            LogFile           = Require(props, "log.file");
            ThreadCount       = RequirePositiveInt(props, "producer.threads");
            MessagesPerThread = RequirePositiveInt(props, "producer.messagesPerThread");

            ReconnectCount = RequireNonNegativeOrMinusOneInt(props, "mq.reconnectCount");
            ReconnectDelay = RequireNonNegativeInt(props, "mq.reconnectDelaySeconds");
            CcsId          = RequirePositiveInt(props, "mq.ccsid");
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static Dictionary<string, string> LoadProperties(string path)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#') || line.StartsWith('!'))
                    continue;

                int eq = line.IndexOf('=');
                if (eq < 0)
                    continue;

                string key   = line[..eq].Trim();
                string value = line[(eq + 1)..].Trim();
                result[key] = value;
            }
            return result;
        }

        private static string Require(Dictionary<string, string> props, string key)
        {
            if (!props.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Missing required property: {key}");
            return value.Trim();
        }

        private static int RequirePositiveInt(Dictionary<string, string> props, string key)
        {
            string raw = Require(props, key);
            if (!int.TryParse(raw, out int value))
                throw new ArgumentException($"Property '{key}' must be an integer, got: {raw}");
            if (value <= 0)
                throw new ArgumentException($"Property '{key}' must be positive, got: {value}");
            return value;
        }

        /// <summary>Parses a non-negative integer (0 is allowed).</summary>
        private static int RequireNonNegativeInt(Dictionary<string, string> props, string key)
        {
            string raw = Require(props, key);
            if (!int.TryParse(raw, out int value))
                throw new ArgumentException($"Property '{key}' must be an integer, got: {raw}");
            if (value < 0)
                throw new ArgumentException($"Property '{key}' must be >= 0, got: {value}");
            return value;
        }

        /// <summary>Parses a non-negative integer or -1 (retry-indefinitely sentinel).</summary>
        private static int RequireNonNegativeOrMinusOneInt(Dictionary<string, string> props, string key)
        {
            string raw = Require(props, key);
            if (!int.TryParse(raw, out int value))
                throw new ArgumentException($"Property '{key}' must be an integer, got: {raw}");
            if (value < -1)
                throw new ArgumentException($"Property '{key}' must be >= -1, got: {value}");
            return value;
        }
    }
}
