package com.example.mqproducer;

import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * Application entry point for the IBM MQ Jakarta JMS producer test harness.
 * <p>
 * Usage:
 * <pre>
 *   java -cp "out;C:\MQ\MQ10.0\java\lib\modules\jakarta\*" com.example.mqproducer.Main [properties-file]
 * </pre>
 * If no argument is supplied the default path {@code producer.properties} is used.
 */
public class Main {

    private static final Logger LOG = Logger.getLogger(Main.class.getName());

    public static void main(String[] args) {
        String propertiesPath = (args.length > 0) ? args[0] : "producer.properties";

        ProducerConfig config;
        try {
            config = new ProducerConfig(propertiesPath);
            LoggingSetup.init(config.getLogFile());
        } catch (Exception e) {
            System.err.println("ERROR: Failed to initialise: " + e.getMessage());
            System.exit(1);
            return; // unreachable — satisfies the compiler
        }

        int totalMessages = config.getThreadCount() * config.getMessagesPerThread();
        LOG.info("Starting MQ producer harness — threads=" + config.getThreadCount()
                + "  messagesPerThread=" + config.getMessagesPerThread()
                + "  totalMessages=" + totalMessages
                + "  queue=" + config.getQueue());

        int      threadCount = config.getThreadCount();
        Thread[] threads     = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++) {
            ProducerWorker worker = new ProducerWorker(config, i + 1);
            Thread thread = new Thread(worker, "producer-" + (i + 1));
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

        LOG.info(String.format("All producer threads finished — %d messages sent — program exiting",
                totalMessages));
    }
}
