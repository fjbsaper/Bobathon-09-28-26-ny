using System;
using System.Threading;
using IBM.XMS;

namespace MqConsumer
{
    /// <summary>
    /// A single consumer thread.
    /// Each instance opens its own XMS/JMS connection, consumes messages from the configured queue
    /// until the receive timeout expires (<c>Receive()</c> returns <c>null</c>), then closes all
    /// XMS resources and exits.
    /// </summary>
    public sealed class ConsumerWorker
    {
        private static readonly AppLogger Log = AppLogger.GetLogger(typeof(ConsumerWorker).FullName!);

        private readonly AppConfig _config;
        private readonly int       _workerId;

        /// <param name="config">Shared (read-only) application configuration.</param>
        /// <param name="workerId">1-based identifier used only in log messages.</param>
        public ConsumerWorker(AppConfig config, int workerId)
        {
            _config   = config;
            _workerId = workerId;
        }

        /// <summary>Entry point executed on the worker thread.</summary>
        public void Run()
        {
            Log.Info($"Worker {_workerId} starting");

            IConnection?      connection = null;
            ISession?         session    = null;
            IMessageConsumer? consumer   = null;

            try
            {
                // Build the connection factory using CCDT for channel/host resolution
                XMSFactoryFactory factoryFactory = XMSFactoryFactory.GetInstance(XMSC.CT_WMQ);
                IConnectionFactory factory       = factoryFactory.CreateConnectionFactory();

                factory.SetStringProperty(XMSC.WMQ_CCDTURL,       _config.CcdtUrl);
                factory.SetStringProperty(XMSC.WMQ_QUEUE_MANAGER,  _config.QueueManager);
                factory.SetIntProperty(XMSC.WMQ_CONNECTION_MODE,   XMSC.WMQ_CM_CLIENT);

                // Automatic client reconnect — reconnect to ANY queue manager in the CCDT
                factory.SetIntProperty(XMSC.WMQ_CLIENT_RECONNECT_OPTIONS, XMSC.WMQ_CLIENT_RECONNECT_ANY);
                // Total seconds the client will keep retrying (-1 = indefinite)
                factory.SetIntProperty(XMSC.WMQ_CLIENT_RECONNECT_TIMEOUT, _config.ReconnectCount);

                // CCSID 1208 = UTF-8 character encoding for all messages
                factory.SetIntProperty(XMSC.WMQ_CCSID, _config.CcsId);

                connection = factory.CreateConnection();
                session    = connection.CreateSession(false, AcknowledgeMode.AutoAcknowledge);

                IDestination destination = session.CreateQueue(_config.Queue);
                consumer = session.CreateConsumer(destination);

                connection.Start();

                Log.Info($"Worker {_workerId} connected; waiting for messages on {_config.Queue}");

                // Receive loop — exits when Receive() returns null (timeout reached)
                while (true)
                {
                    IMessage? message = consumer.Receive(_config.TimeoutMs);
                    if (message == null)
                    {
                        Log.Info($"Worker {_workerId} timed out — no message received within "
                                 + $"{_config.TimeoutMs / 1000}s; exiting");
                        break;
                    }
                    LogMessage(message);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Worker {_workerId} encountered an error", ex);
            }
            finally
            {
                CloseQuietly(consumer, session, connection);
                Log.Info($"Worker {_workerId} stopped");
            }
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private void LogMessage(IMessage message)
        {
            try
            {
                string dest    = DestinationName(message.JMSDestination);
                string replyTo = DestinationName(message.JMSReplyTo);
                string body    = (message is ITextMessage tm)
                                 ? tm.Text
                                 : $"<non-text message: {message.GetType().Name}>";

                Log.Info($"Received  destination={dest}  replyTo={replyTo}  body=[{body}]");
            }
            catch (Exception ex)
            {
                Log.Warn($"Worker {_workerId}: failed to log message details: {ex.Message}");
            }
        }

        private static string DestinationName(IDestination? dest)
        {
            if (dest == null) return "<none>";
            try   { return dest.ToString() ?? "<none>"; }
            catch { return "<error reading destination>"; }
        }

        private static void CloseQuietly(IMessageConsumer? consumer, ISession? session,
                                         IConnection? connection)
        {
            try { consumer?.Close();   } catch { /* best effort */ }
            try { session?.Close();    } catch { /* best effort */ }
            try { connection?.Close(); } catch { /* best effort */ }
        }
    }
}
