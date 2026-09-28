package com.example.mqconsumer;

import com.ibm.mq.jakarta.jms.MQConnectionFactory;
import com.ibm.msg.client.jakarta.wmq.WMQConstants;
import jakarta.jms.Connection;
import jakarta.jms.Destination;
import jakarta.jms.MessageConsumer;
import jakarta.jms.Session;
import jakarta.jms.TextMessage;

import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * A single consumer thread.
 * Each instance opens its own JMS connection, consumes messages from the configured queue
 * until the receive timeout expires (i.e. {@code receive()} returns {@code null}), then
 * closes all JMS resources and exits.
 */
public class ConsumerWorker implements Runnable {

    private static final Logger LOG = Logger.getLogger(ConsumerWorker.class.getName());

    private final AppConfig config;
    private final int workerId;

    /**
     * @param config   shared (read-only) application configuration
     * @param workerId 1-based identifier used only in log messages
     */
    public ConsumerWorker(AppConfig config, int workerId) {
        this.config   = config;
        this.workerId = workerId;
    }

    @Override
    public void run() {
        LOG.info("Worker " + workerId + " starting");

        Connection      connection = null;
        Session         session    = null;
        MessageConsumer consumer   = null;

        try {
            // Build the connection factory using CCDT for channel/host resolution
            MQConnectionFactory factory = new MQConnectionFactory();
            factory.setStringProperty(WMQConstants.WMQ_CCDTURL,       config.getCcdtUrl());
            factory.setStringProperty(WMQConstants.WMQ_QUEUE_MANAGER, config.getQueueManager());

            // Automatic client reconnect — reconnect to ANY queue manager in the CCDT
            factory.setIntProperty(WMQConstants.WMQ_CLIENT_RECONNECT_OPTIONS,
                                   WMQConstants.WMQ_CLIENT_RECONNECT);
            // Total seconds the client will keep retrying (-1 = indefinite)
            factory.setIntProperty(WMQConstants.WMQ_CLIENT_RECONNECT_TIMEOUT,
                                   config.getReconnectCount());

            // CCSID 1208 = UTF-8 character encoding for all messages
            factory.setIntProperty(WMQConstants.WMQ_CCSID, config.getCcsId());

            connection = factory.createConnection();
            session    = connection.createSession(false, Session.AUTO_ACKNOWLEDGE);

            Destination destination = session.createQueue(config.getQueue());
            consumer = session.createConsumer(destination);

            connection.start();

            LOG.info("Worker " + workerId + " connected; waiting for messages on " + config.getQueue());

            // Receive loop — exits when receive() returns null (timeout reached)
            while (true) {
                try {
                jakarta.jms.Message message = consumer.receive(config.getTimeoutMillis());
                /*
                if (message == null) {
                    LOG.info("Worker " + workerId + " timed out — no message received within "
                            + (config.getTimeoutMillis() / 1000) + "s; exiting");
                    break;
                }
                */
                logMessage(message);
                } catch (Exception e2) {
                    LOG.severe("JMSException " + e2.getMessage());
                }
            }

        } catch (Exception e) {
            LOG.log(Level.SEVERE, "Worker " + workerId + " encountered an error", e);
        } finally {
            closeQuietly(consumer, session, connection);
            LOG.info("Worker " + workerId + " stopped");
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private void logMessage(jakarta.jms.Message message) {
        try {
            String dest    = destinationName(message.getJMSDestination());
            String replyTo = destinationName(message.getJMSReplyTo());
            String body    = (message instanceof TextMessage)
                             ? ((TextMessage) message).getText()
                             : "<non-text message: " + message.getClass().getSimpleName() + ">";

            LOG.info(String.format("Received  destination=%s  replyTo=%s  body=[%s]",
                    dest, replyTo, body));
        } catch (Exception e) {
            LOG.log(Level.WARNING, "Worker " + workerId + ": failed to log message details", e);
        }
    }

    private static String destinationName(Destination dest) {
        if (dest == null) {
            return "<none>";
        }
        try {
            // MQDestination exposes toString() with the queue name; use it as the display value
            return dest.toString();
        } catch (Exception e) {
            return "<error reading destination>";
        }
    }

    private static void closeQuietly(MessageConsumer consumer, Session session,
                                     Connection connection) {
        if (consumer != null) {
            try { consumer.close(); } catch (Exception ignored) { /* best effort */ }
        }
        if (session != null) {
            try { session.close(); } catch (Exception ignored) { /* best effort */ }
        }
        if (connection != null) {
            try { connection.close(); } catch (Exception ignored) { /* best effort */ }
        }
    }
}
