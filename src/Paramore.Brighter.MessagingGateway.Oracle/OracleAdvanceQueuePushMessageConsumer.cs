// using System;
// using System.Text;
// using System.Threading;
// using System.Threading.Tasks;
// using Oracle.ManagedDataAccess.Client;
//
// namespace Paramore.Brighter.MessagingGateway.Oracle;
//
// public class OracleAdvanceQueuePushMessageConsumer(string connectionString, 
//     OracleAdvanceQueueSubscription subscription
//     // , Encoding encoding
//     ) : IAmAMessageConsumerSync, IAmAMessageConsumerAsync
// {
//     private Message? _message;
//     private OracleConnection? _connection;
//     private OracleAQQueue? _queue;
//
//     /// <inheritdoc />
//     public void Acknowledge(Message message) => _connection?.Commit();
//     
//     /// <inheritdoc />
//     public Task AcknowledgeAsync(Message message, CancellationToken cancellationToken = default(CancellationToken))
//     {
//         Acknowledge(message);
//         return Task.CompletedTask;
//     }
//
//     /// <inheritdoc />
//     public bool Reject(Message message)
//     {
//         _connection?.Rollback();
//         return true;
//     }
//     
//     /// <inheritdoc />
//     public Task<bool> RejectAsync(Message message, CancellationToken cancellationToken = default) 
//         => Task.FromResult(Reject(message));
//
//     public void Purge()
//     {
//         throw new NotImplementedException();
//     }
//
//     /// <inheritdoc />
//     public Message[] Receive(TimeSpan? timeOut = null)
//     {
//         if (_message != null)
//         {
//             var tmp = _message;
//             _message = null;
//             return [tmp];
//         }
//
//         if (_connection != null)
//         {
//             return [new Message()];
//         }
//
//         _connection = new OracleConnection(connectionString);
//         _connection.Open();
//         
//         _queue = new OracleAQQueue(subscription.ChannelName, _connection)
//         {
//             UdtTypeName = subscription.UdtTypeName,
//             MessageType = subscription.MessageType,
//             // Notification = { GroupingInterval = 0 },
//             DequeueOptions = new OracleAQDequeueOptions
//             {
//                 ConsumerName = subscription.ConsumerName,
//                 Correlation = subscription.Correlation,
//                 DeliveryMode = subscription.DeliveryMode,
//                 DequeueMode = subscription.DequeueMode,
//                 MessageId = new byte[subscription.MessageIdLength],
//                 NavigationMode = subscription.NavigationMode,
//                 ProviderSpecificType = subscription.ProviderSpecificType,
//                 Visibility = subscription.Visibility,
//                 Wait = (int)subscription.TimeOut.TotalMilliseconds
//             }
//         };
//
//         _queue.MessageAvailable += OnMessageReceived;
//         return [new Message()];
//     }
//     
//     /// <inheritdoc />
//     public async Task<Message[]> ReceiveAsync(TimeSpan? timeOut = null, CancellationToken cancellationToken = default(CancellationToken))
//     {
//         if (_message != null)
//         {
//             var tmp = _message;
//             _message = null;
//             return [tmp];
//         }
//
//         if (_connection != null)
//         {
//             return [new Message()];
//         }
//
//         _connection = new OracleConnection(connectionString);
//         await _connection.OpenAsync(cancellationToken);
//         
//         _queue = new OracleAQQueue(subscription.ChannelName, _connection)
//         {
//             UdtTypeName = subscription.UdtTypeName,
//             MessageType = subscription.MessageType,
//             // Notification = { GroupingInterval = 0 },
//             DequeueOptions = new OracleAQDequeueOptions
//             {
//                 ConsumerName = subscription.ConsumerName,
//                 Correlation = subscription.Correlation,
//                 DeliveryMode = subscription.DeliveryMode,
//                 DequeueMode = subscription.DequeueMode,
//                 MessageId = new byte[subscription.MessageIdLength],
//                 NavigationMode = subscription.NavigationMode,
//                 ProviderSpecificType = subscription.ProviderSpecificType,
//                 Visibility = subscription.Visibility,
//                 Wait = (int)subscription.TimeOut.TotalMilliseconds
//             }
//         };
//
//         _queue.MessageAvailable += OnMessageReceived;
//         return [new Message()];
//     }
//
//     /// <inheritdoc />
//     public bool Requeue(Message message, TimeSpan? delay = null)
//     {
//         if (!message.Header.Bag.TryGetValue(HeaderNames.ReceiptHandler, out object? tmp)
//             || tmp is not OracleAQMessage oracleAqMessage)
//         {
//             return false;
//         }
//
//         if (_queue != null && _connection != null)
//         {
//             // oracleAqMessage.EnqueueTime
//             _queue.Enqueue(oracleAqMessage);
//             _connection.Commit();
//         }
//
//         return true;
//     }
//     
//     /// <inheritdoc />
//     public Task<bool> RequeueAsync(Message message, TimeSpan? delay = null, CancellationToken cancellationToken = default)
//         => Task.FromResult(Requeue(message, delay));
//
//     private void OnMessageReceived(object sender, OracleAQMessageAvailableEventArgs e)
//     {
//         
//     }
//     
//     public void Dispose()
//     {
//         if (_queue != null)
//         {
//             _queue.MessageAvailable -= OnMessageReceived;
//         }
//
//         _queue?.Dispose();
//         _connection?.Dispose();
//     }
//
// #if NETFRAMEWORK
//     public ValueTask DisposeAsync()
//     {
//         _message = null;
//         Dispose();
//         return new ValueTask();
//     }
// #else
//     public async ValueTask DisposeAsync()
//     {
//         _message = null;
//         if (_queue != null)
//         {
//             _queue.MessageAvailable -= OnMessageReceived;
//             _queue.Dispose();
//         }
//
//         if (_connection != null)
//         {
//             await _connection.DisposeAsync();
//         }
//     }
// #endif
//
//     public Task PurgeAsync(CancellationToken cancellationToken = default(CancellationToken))
//     {
//         throw new NotImplementedException();
//     }
// }
