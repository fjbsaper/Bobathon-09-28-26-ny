using System;
using System.Threading;

namespace MqProducer
{
    /// <summary>
    /// Application entry point for the IBM MQ XMS .NET producer test harness.
    /// <para>
    /// Usage:
    /// <code>
    ///   dotnet run [properties-file]
    ///   -- or after publish --
    ///   MqProducer.exe [properties-file]
    /// </code>
    /// If no argument is supplied the default path <c>producer.properties</c> is used.
    /// </para>
    /// </summary>
    internal static class Program
    {
        private static AppLogger? _log;

        private static void Main(string[] args)
        {
            string propertiesPath = args.Length > 0 ? args[0] : "producer.properties";

            ProducerConfig config;
            try
            {
                config = new ProducerConfig(propertiesPath);
                AppLogger.Init(config.LogFile);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: Failed to initialise: {ex.Message}");
                Environment.Exit(1);
                return; // unreachable — satisfies the compiler
            }

            _log = AppLogger.GetLogger(typeof(Program).FullName!);

            int totalMessages = config.ThreadCount * config.MessagesPerThread;
            _log.Info($"Starting MQ producer harness — threads={config.ThreadCount}"
                      + $"  messagesPerThread={config.MessagesPerThread}"
                      + $"  totalMessages={totalMessages}"
                      + $"  queue={config.Queue}");

            int      threadCount = config.ThreadCount;
            Thread[] threads     = new Thread[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                var worker   = new ProducerWorker(config, i + 1);
                int captured = i; // capture for lambda
                var thread   = new Thread(() => worker.Run())
                {
                    Name         = $"producer-{captured + 1}",
                    IsBackground = false
                };
                threads[i] = thread;
            }

            // Start all threads
            foreach (var thread in threads)
                thread.Start();

            // Wait for all threads to finish
            foreach (var thread in threads)
            {
                try
                {
                    thread.Join();
                }
                catch (ThreadInterruptedException)
                {
                    Thread.CurrentThread.Interrupt();
                    _log.Warn($"Interrupted while waiting for {thread.Name}");
                }
            }

            _log.Info($"All producer threads finished — {totalMessages} messages sent — program exiting");
        }
    }
}
