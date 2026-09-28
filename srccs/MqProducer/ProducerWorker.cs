using System;
using System.Threading;
using IBM.XMS;

namespace MqProducer
{
    /// <summary>
    /// A single producer thread.
    /// <para>
    /// Each instance opens its own XMS connection, sends exactly
    /// <c>ProducerConfig.MessagesPerThread</c> text messages to the configured queue,
    /// then closes all XMS resources and exits.
    /// </para>
    ///
    /// <para><b>Message body format:</b></para>
    /// <code>
    ///   destination=&lt;queueName&gt; threadId=&lt;id&gt; msg=&lt;sequenceNumber&gt;
    /// </code>
    /// Example: <c>destination=DEV.QUEUE.1 threadId=42 msg=3</c>
    /// <para>
    /// The sequence number is 1-based, counting from 1 to <c>MessagesPerThread</c>.
    /// Thread ID is the managed thread ID from <see cref="Thread.CurrentThread"/>.
    /// </para>
    /// </summary>
    public sealed class ProducerWorker
    {
        private static readonly AppLogger Log = AppLogger.GetLogger(typeof(ProducerWorker).FullName!);

        private readonly ProducerConfig _config;
        private readonly int            _workerId;

        /// <param name="config">Shared (read-only) producer configuration.</param>
        /// <param name="workerId">1-based identifier used only in log messages.</param>
        public ProducerWorker(ProducerConfig config, int workerId)
        {
            _config   = config;
            _workerId = workerId;
        }

        /// <summary>Entry point executed on the worker thread.</summary>
        public void Run()
        {
            Log.Info($"Producer {_workerId} starting");

            IConnection?      connection = null;
            ISession?         session    = null;
            IMessageProducer? producer   = null;

            try
            {
                // Build the connection factory using CCDT for channel/host resolution
                XMSFactoryFactory factoryFactory = XMSFactoryFactory.GetInstance(XMSC.CT_WMQ);
                IConnectionFactory factory       = factoryFactory.CreateConnectionFactory();

                factory.SetStringProperty(XMSC.WMQ_CCDTURL,      _config.CcdtUrl);
                factory.SetStringProperty(XMSC.WMQ_QUEUE_MANAGER, _config.QueueManager);
                factory.SetIntProperty(XMSC.WMQ_CONNECTION_MODE,  XMSC.WMQ_CM_CLIENT);

                // Automatic client reconnect — reconnect to ANY queue manager in the CCDT
                factory.SetIntProperty(XMSC.WMQ_CLIENT_RECONNECT_OPTIONS, XMSC.WMQ_CLIENT_RECONNECT_ANY);
                // Total seconds the client will keep retrying (-1 = indefinite)
                factory.SetIntProperty(XMSC.WMQ_CLIENT_RECONNECT_TIMEOUT, _config.ReconnectCount);

                // CCSID 1208 = UTF-8 character encoding for all messages
                factory.SetIntProperty(XMSC.WMQ_CCSID, _config.CcsId);

                connection = factory.CreateConnection();
                session    = connection.CreateSession(false, AcknowledgeMode.AutoAcknowledge);

                IDestination destination = session.CreateQueue(_config.Queue);
                producer = session.CreateProducer(destination);

                connection.Start();

                int    threadId = Thread.CurrentThread.ManagedThreadId;
                string queue    = _config.Queue;

                for (int n = 1; n <= _config.MessagesPerThread; n++)
                {
                    // Build message body: destination=<queue> threadId=<id> msg=<n>
                    string body = $"destination={queue} threadId={threadId} msg={n}";

                    ITextMessage message = session.CreateTextMessage(body);
                    producer.Send(message);

                    Log.Info($"Sent destination={queue} threadId={threadId} msg={n} body=[{body}]");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Producer {_workerId} encountered an error", ex);
            }
            finally
            {
                CloseQuietly(producer, session, connection);
                int threadId = Thread.CurrentThread.ManagedThreadId;
                Log.Info($"Producer thread {threadId} finished");
            }
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static void CloseQuietly(IMessageProducer? producer, ISession? session,
                                         IConnection? connection)
        {
            try { producer?.Close();   } catch { /* best effort */ }
            try { session?.Close();    } catch { /* best effort */ }
            try { connection?.Close(); } catch { /* best effort */ }
        }
    }
}
