package com.example.mqproducer;

import com.ibm.mq.jakarta.jms.MQConnectionFactory;
import com.ibm.msg.client.jakarta.wmq.WMQConstants;
import jakarta.jms.Connection;
import jakarta.jms.Destination;
import jakarta.jms.MessageProducer;
import jakarta.jms.Session;
import jakarta.jms.TextMessage;

import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * A single producer thread.
 * <p>
 * Each instance opens its own JMS connection, sends exactly
 * {@code ProducerConfig.getMessagesPerThread()} text messages to the configured queue,
 * then closes all JMS resources and exits.
 *
 * <h3>Message body format</h3>
 * <pre>
 *   destination=&lt;queueName&gt; threadId=&lt;id&gt; msg=&lt;sequenceNumber&gt;
 * </pre>
 * Example: {@code destination=DEV.QUEUE.1 threadId=42 msg=3}
 * <p>
 * The sequence number is 1-based, counting from 1 to {@code messagesPerThread} within
 * each thread. Thread ID is obtained via {@link Thread#getId()} (Java 17 compatible).
 */
public class ProducerWorker implements Runnable {

    private static final Logger LOG = Logger.getLogger(ProducerWorker.class.getName());

    private final ProducerConfig config;
    private final int workerId;

    /**
     * @param config   shared (read-only) producer configuration
     * @param workerId 1-based identifier used only in log messages
     */
    public ProducerWorker(ProducerConfig config, int workerId) {
        this.config   = config;
        this.workerId = workerId;
    }

    @Override
    public void run() {
        LOG.info("Producer " + workerId + " starting");

        Connection      connection = null;
        Session         session    = null;
        MessageProducer producer   = null;

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
            producer = session.createProducer(destination);

            connection.start();

            long   threadId = Thread.currentThread().threadId();
            String queue    = config.getQueue();

            for (int n = 1; n <= config.getMessagesPerThread(); n++) {
                // Build message body: destination=<queue> threadId=<id> msg=<n>
                String body = String.format("destination=%s threadId=%d msg=%d",
                        queue, threadId, n);

                TextMessage message = session.createTextMessage(body);
                producer.send(message);

                LOG.info(String.format("Sent destination=%s threadId=%d msg=%d body=[%s]",
                        queue, threadId, n, body));
                try {
                    Thread.sleep(1000);
                } catch (InterruptedException ie) {
                    //do nothing                    
                }       
            }

        } catch (Exception e) {
            LOG.log(Level.SEVERE, "Producer " + workerId + " encountered an error", e);
        } finally {
            closeQuietly(producer, session, connection);
            long threadId = Thread.currentThread().threadId();
            LOG.info(String.format("Producer thread %d finished", threadId));
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static void closeQuietly(MessageProducer producer, Session session,
                                     Connection connection) {
        if (producer != null) {
            try { producer.close(); } catch (Exception ignored) { /* best effort */ }
        }
        if (session != null) {
            try { session.close(); } catch (Exception ignored) { /* best effort */ }
        }
        if (connection != null) {
            try { connection.close(); } catch (Exception ignored) { /* best effort */ }
        }
    }
}
