package com.example.mqconsumer;

import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * Application entry point for the IBM MQ Jakarta JMS consumer.
 * <p>
 * Usage:
 * <pre>
 *   java -cp "out;C:\MQ\MQ10.0\java\lib\modules\jakarta\*" com.example.mqconsumer.Main [properties-file]
 * </pre>
 * If no argument is supplied the default path {@code consumer.properties} is used.
 */
public class Main {

    private static final Logger LOG = Logger.getLogger(Main.class.getName());

    public static void main(String[] args) {
        String propertiesPath = (args.length > 0) ? args[0] : "consumer.properties";

        AppConfig config;
        try {
            config = new AppConfig(propertiesPath);
            LoggingSetup.init(config.getLogFile());
        } catch (Exception e) {
            System.err.println("ERROR: Failed to initialise: " + e.getMessage());
            System.exit(1);
            return; // unreachable — satisfies the compiler
        }

        LOG.info("Starting MQ consumer — threads=" + config.getThreadCount()
                + "  queue=" + config.getQueue()
                + "  timeout=" + (config.getTimeoutMillis() / 1000) + "s");

        int      threadCount = config.getThreadCount();
        Thread[] threads     = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++) {
            ConsumerWorker worker = new ConsumerWorker(config, i + 1);
            Thread thread = new Thread(worker, "worker-" + (i + 1));
            threads[i] = thread;
        }

        // Start all threads
        for (Thread thread : threads) {
            thread.start();
        }

        // Wait for all threads to finish
        for (Thread thread : threads) {
            try {
                thread.join();
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                LOG.log(Level.WARNING, "Interrupted while waiting for " + thread.getName(), e);
            }
        }

        LOG.info("All consumer threads finished — program exiting");
    }
}
