using System;
using System.Threading;

namespace MqConsumer
{
    /// <summary>
    /// Application entry point for the IBM MQ XMS .NET consumer.
    /// <para>
    /// Usage:
    /// <code>
    ///   dotnet run [properties-file]
    ///   -- or after publish --
    ///   MqConsumer.exe [properties-file]
    /// </code>
    /// If no argument is supplied the default path <c>consumer.properties</c> is used.
    /// </para>
    /// </summary>
    internal static class Program
    {
        private static AppLogger? _log;

        private static void Main(string[] args)
        {
            string propertiesPath = args.Length > 0 ? args[0] : "consumer.properties";

            AppConfig config;
            try
            {
                config = new AppConfig(propertiesPath);
                AppLogger.Init(config.LogFile);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: Failed to initialise: {ex.Message}");
                Environment.Exit(1);
                return; // unreachable — satisfies the compiler
            }

            _log = AppLogger.GetLogger(typeof(Program).FullName!);
            _log.Info($"Starting MQ consumer — threads={config.ThreadCount}"
                      + $"  queue={config.Queue}"
                      + $"  timeout={config.TimeoutMs / 1000}s");

            int       threadCount = config.ThreadCount;
            Thread[]  threads     = new Thread[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                var worker   = new ConsumerWorker(config, i + 1);
                int captured = i; // capture for lambda
                var thread   = new Thread(() => worker.Run())
                {
                    Name         = $"worker-{captured + 1}",
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

            _log.Info("All consumer threads finished — program exiting");
        }
    }
}
