package com.example.mqproducer;

import java.io.FileInputStream;
import java.io.IOException;
import java.util.Properties;

/**
 * Loads and validates producer.properties.
 * All runtime configuration is accessed through typed getters on this class;
 * no other class opens files or parses strings directly.
 */
public class ProducerConfig {

    private final String ccdtUrl;
    private final String queueManager;
    private final String queue;
    private final int    threadCount;
    private final int    messagesPerThread;
    private final String logFile;
    /** Total reconnect timeout in seconds passed to {@code WMQ_CLIENT_RECONNECT_TIMEOUT}. -1 = indefinite. */
    private final int    reconnectCount;
    /** Approximate delay between reconnect attempts (informational). */
    private final int    reconnectDelaySeconds;
    /** CCSID for message encoding — 1208 = UTF-8. Passed to {@code WMQ_CCSID}. */
    private final int    ccsId;

    /**
     * @param propertiesPath path to the producer.properties file
     * @throws IOException              if the file cannot be read
     * @throws IllegalArgumentException if a required key is missing or has an invalid value
     */
    public ProducerConfig(String propertiesPath) throws IOException {
        Properties props = new Properties();
        try (FileInputStream fis = new FileInputStream(propertiesPath)) {
            props.load(fis);
        }

        ccdtUrl           = require(props, "mq.ccdtUrl");
        queueManager      = require(props, "mq.queueManager");
        queue             = require(props, "mq.queue");
        logFile           = require(props, "log.file");
        threadCount       = requirePositiveInt(props, "producer.threads");
        messagesPerThread = requirePositiveInt(props, "producer.messagesPerThread");

        reconnectCount        = requireNonNegativeOrMinusOne(props, "mq.reconnectCount");
        reconnectDelaySeconds = requireNonNegative(props, "mq.reconnectDelaySeconds");
        ccsId                 = requirePositiveInt(props, "mq.ccsid");
    }

    // -----------------------------------------------------------------------
    // Getters
    // -----------------------------------------------------------------------

    /** CCDT file URL (e.g. {@code file:///C:/MQ/ccdt.json}). */
    public String getCcdtUrl() { return ccdtUrl; }

    /** Queue manager name. */
    public String getQueueManager() { return queueManager; }

    /** Destination queue name. */
    public String getQueue() { return queue; }

    /** Number of producer threads. */
    public int getThreadCount() { return threadCount; }

    /** Number of messages each thread sends. */
    public int getMessagesPerThread() { return messagesPerThread; }

    /** Path to the log file. */
    public String getLogFile() { return logFile; }

    /**
     * Total reconnect timeout in seconds for {@code WMQ_CLIENT_RECONNECT_TIMEOUT}.
     * -1 means retry indefinitely.
     */
    public int getReconnectCount() { return reconnectCount; }

    /** Approximate delay in seconds between reconnect attempts (informational). */
    public int getReconnectDelaySeconds() { return reconnectDelaySeconds; }

    /** CCSID for message encoding — 1208 = UTF-8. */
    public int getCcsId() { return ccsId; }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static String require(Properties props, String key) {
        String value = props.getProperty(key);
        if (value == null || value.isBlank()) {
            throw new IllegalArgumentException("Missing required property: " + key);
        }
        return value.trim();
    }

    private static int requirePositiveInt(Properties props, String key) {
        String raw = require(props, key);
        int value;
        try {
            value = Integer.parseInt(raw);
        } catch (NumberFormatException e) {
            throw new IllegalArgumentException(
                    "Property '" + key + "' must be an integer, got: " + raw);
        }
        if (value <= 0) {
            throw new IllegalArgumentException(
                    "Property '" + key + "' must be positive, got: " + value);
        }
        return value;
    }

    /** Parses a non-negative integer (0 is allowed). */
    private static int requireNonNegative(Properties props, String key) {
        String raw = require(props, key);
        int value;
        try {
            value = Integer.parseInt(raw);
        } catch (NumberFormatException e) {
            throw new IllegalArgumentException(
                    "Property '" + key + "' must be an integer, got: " + raw);
        }
        if (value < 0) {
            throw new IllegalArgumentException(
                    "Property '" + key + "' must be >= 0, got: " + value);
        }
        return value;
    }

    /** Parses a non-negative integer or -1 (retry-indefinitely sentinel). */
    private static int requireNonNegativeOrMinusOne(Properties props, String key) {
        String raw = require(props, key);
        int value;
        try {
            value = Integer.parseInt(raw);
        } catch (NumberFormatException e) {
            throw new IllegalArgumentException(
                    "Property '" + key + "' must be an integer, got: " + raw);
        }
        if (value < -1) {
            throw new IllegalArgumentException(
                    "Property '" + key + "' must be >= -1, got: " + value);
        }
        return value;
    }
}
