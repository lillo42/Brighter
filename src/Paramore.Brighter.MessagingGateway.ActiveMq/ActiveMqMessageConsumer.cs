using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

/// <summary>
/// Implements a message consumer for ActiveMQ (using the NMS client) that is compliant
/// with Brighter's synchronous and asynchronous consumer interfaces.
/// </summary>
/// <remarks>
/// This consumer uses a **transactional** NMS session to ensure atomic message handling
/// for <see cref="Acknowledge(Message)"/>, <see cref="Reject(Message)"/>, and <see cref="Requeue(Message, TimeSpan?)"/> 
/// operations, relying on <see cref="ISession.Commit()"/> for acknowledge/reject and 
/// <see cref="ISession.Rollback()"/> for requeue.
/// </remarks>
public class ActiveMqMessageConsumer : IAmAMessageConsumerSync, IAmAMessageConsumerAsync
{
    private readonly ActiveMqSubscription _subscription;
    private readonly ISession _session;
    private readonly IDestination _queue;
    private readonly IMessageConsumer _consumer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveMqMessageConsumer"/> class.
    /// </summary>
    /// <param name="connection">The established ActiveMQ connection.</param>
    /// <param name="subscription">The ActiveMQ subscription details, which determine the destination and consumer type.</param>
    public ActiveMqMessageConsumer(IConnection connection, ActiveMqSubscription subscription)
    {
        _subscription = subscription;
        
        // ActiveMQ consumers for Brighter use a transactional session for explicit control over acknowledge/requeue.
        _session = connection.CreateSession(AcknowledgementMode.Transactional);
        _queue = CreateDestination(_session, _subscription);
        _consumer = CreateConsumer(_session, _queue, _subscription);
    }

    /// <summary>
    /// Acknowledges a message synchronously. This commits the transactional session.
    /// </summary>
    /// <param name="message">The message to acknowledge.</param>
    public void Acknowledge(Message message)
    {
        _session.Commit();
    }
    
    /// <summary>
    /// Acknowledges a message asynchronously. This commits the transactional session.
    /// </summary>
    /// <param name="message">The message to acknowledge.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous commit operation.</returns>
    public async Task AcknowledgeAsync(Message message, CancellationToken cancellationToken = default)
    {
        await _session.CommitAsync();
    }

    /// <summary>
    /// Rejects a message synchronously. Due to the transactional nature, a rejected message
    /// is not delivered again and is committed out of the queue in this implementation.
    /// </summary>
    /// <param name="message">The message to reject.</param>
    /// <returns>Always returns <see langword="true"/>.</returns>
    public bool Reject(Message message)
    {
        // ActiveMQ rejection is handled by committing the transaction without explicit NMS reject
        _session.Commit();
        return true;
    }
    
    /// <summary>
    /// Rejects a message asynchronously. Due to the transactional nature, a rejected message
    /// is not delivered again and is committed out of the queue in this implementation.
    /// </summary>
    /// <param name="message">The message to reject.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that returns <see langword="true"/>.</returns>
    public async Task<bool> RejectAsync(Message message, CancellationToken cancellationToken = default)
    {
        // ActiveMQ rejection is handled by committing the transaction without explicit NMS reject
        await _session.CommitAsync();
        return true;
    }

    /// <summary>
    /// Synchronously purges all currently available messages from the destination.
    /// </summary>
    public void Purge()
    {
        // Continuously receive and commit (acknowledge) messages until the queue is empty
        while (true)
        {
            var message = _consumer.Receive(TimeSpan.FromSeconds(1));
            if (message == null)
            {
                break;
            }
            
            _session.Commit();
        }
    }
    
    /// <summary>
    /// Asynchronously purges all currently available messages from the destination.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous purge operation.</returns>
    public async Task PurgeAsync(CancellationToken cancellationToken = default)
    {
        // Continuously receive and commit (acknowledge) messages until the queue is empty
        while (!cancellationToken.IsCancellationRequested)
        {
            var message = await _consumer.ReceiveAsync(TimeSpan.FromSeconds(1));
            if (message == null)
            {
                break;
            }
            
            await _session.CommitAsync();
        }
    }

    /// <summary>
    /// Synchronously receives the next message from the ActiveMQ destination.
    /// </summary>
    /// <param name="timeOut">The time to wait for a message. If <see langword="null"/>, a zero timeout is used.</param>
    /// <returns>An array containing the received <see cref="Message"/>, or a single empty <see cref="Message"/> if a timeout occurs or an error happens.</returns>
    public Message[] Receive(TimeSpan? timeOut = null)
    {
        try
        {
            var message = _consumer.Receive(timeOut ?? TimeSpan.Zero);
            if (message == null)
            {
                return [new Message()];
            }

            return [ToBrighterMessage(message, _subscription)];
        }
        catch
        {
            // On any exception during receive, return an empty message array
            return [new Message()];
        }
    }

    /// <summary>
    /// Factory method to create the appropriate ActiveMQ <see cref="IDestination"/> (Queue or Topic).
    /// </summary>
    /// <param name="session">The NMS session.</param>
    /// <param name="subscription">The subscription details.</param>
    /// <returns>An NMS <see cref="IDestination"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the subscription type is not recognized.</exception>
    private static IDestination CreateDestination(ISession session, ActiveMqSubscription subscription)
    {
        return subscription switch
        {
            ActiveMqQueueSubscription => session.GetQueue(subscription.ChannelName.Value),
            ActiveMqTopicSubscription => session.GetTopic(subscription.RoutingKey.Value),
            _ => throw new InvalidOperationException("Invalid ActiveMqSubscription")
        };
    }

    /// <summary>
    /// Factory method to create the appropriate ActiveMQ <see cref="IMessageConsumer"/>.
    /// </summary>
    /// <param name="session">The NMS session.</param>
    /// <param name="destination">The NMS destination.</param>
    /// <param name="subscription">The subscription details.</param>
    /// <returns>An NMS <see cref="IMessageConsumer"/>.</returns>
    /// <exception cref="ConfigurationException">Thrown if the subscription or consumer type is invalid.</exception>
    private static IMessageConsumer CreateConsumer(ISession session,
        IDestination destination,
        ActiveMqSubscription subscription)
    {
        if (subscription is ActiveMqQueueSubscription)
        {
            return session.CreateConsumer(destination, subscription.Selector, subscription.NoLocal);
        }
        
        if(subscription is ActiveMqTopicSubscription topicSubscription)
        {
            return topicSubscription.ConsumerType switch
            {
                ConsumerType.Default => session.CreateConsumer(destination, subscription.Selector, subscription.NoLocal),
                ConsumerType.Durable => session.CreateDurableConsumer((ITopic)destination, subscription.Name, subscription.Selector, subscription.NoLocal),
                ConsumerType.Share => session.CreateSharedConsumer((ITopic)destination, subscription.Name, subscription.Selector),
                ConsumerType.ShareDurable => session.CreateSharedDurableConsumer((ITopic)destination, subscription.Name, subscription.Selector),
                _ => throw new ConfigurationException("Invalid consumer type")
            };
        }

        throw new ConfigurationException("Invalid ActiveMqSubscription");
    }

    /// <summary>
    /// Asynchronously receives the next message from the ActiveMQ destination.
    /// </summary>
    /// <param name="timeOut">The time to wait for a message. If <see langword="null"/>, a zero timeout is used.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that returns an array containing the received <see cref="Message"/>, or a single empty <see cref="Message"/> if a timeout occurs or an error happens.</returns>
    public async Task<Message[]> ReceiveAsync(TimeSpan? timeOut = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await _consumer.ReceiveAsync(timeOut ?? TimeSpan.Zero);
            if (message == null)
            {
                return [new Message()];
            }

            return [ToBrighterMessage(message, _subscription)];
        }
        catch
        {
            // On any exception during receive, return an empty message array
            return [new Message()];
        }
    }

    /// <summary>
    /// Requeues a message synchronously. This rolls back the transactional session,
    /// causing the message to be redelivered.
    /// </summary>
    /// <param name="message">The message to requeue.</param>
    /// <param name="delay">An optional delay for the requeued message (Note: NMS rollback does not typically support a delay).</param>
    /// <returns>Always returns <see langword="true"/>.</returns>
    public bool Requeue(Message message, TimeSpan? delay = null)
    {
        // Rollback causes the message to be redelivered
        _session.Rollback();
        return true;
    }
    
    /// <summary>
    /// Requeues a message asynchronously. This rolls back the transactional session,
    /// causing the message to be redelivered.
    /// </summary>
    /// <param name="message">The message to requeue.</param>
    /// <param name="delay">An optional delay for the requeued message (Note: NMS rollback does not typically support a delay).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that returns <see langword="true"/>.</returns>
    public async Task<bool> RequeueAsync(Message message, TimeSpan? delay = null,
        CancellationToken cancellationToken = default)
    {
        // Rollback causes the message to be redelivered
        await _session.RollbackAsync();
        return true;
    }

    /// <summary>
    /// Converts an NMS <see cref="IMessage"/> into a Brighter <see cref="Message"/>, mapping headers and body.
    /// </summary>
    /// <param name="message">The NMS message to convert.</param>
    /// <param name="subscription">The subscription details used to infer missing message properties.</param>
    /// <returns>A new <see cref="Message"/> instance.</returns>
    /// <exception cref="NotSupportedException">Thrown if the NMS message type is not <see cref="IBytesMessage"/> or <see cref="ITextMessage"/>.</exception>
    private static Message ToBrighterMessage(IMessage message, ActiveMqSubscription subscription)
    {
        var bag = new Dictionary<string, object>();
        
        // Copy all NMS properties to the Brighter bag
        foreach (var key in message.Properties.Keys)
        {
            if (key is not string keyString)
            {
                continue;
            }

            bag[keyString] = message.Properties[keyString];
        }

        // Add the NMS message object itself to the bag as the receipt handle
        bag[HeaderNames.ReceiptHandle] = message;
        
        var header = new MessageHeader(
            messageId: GetId(message),
            topic: GetTopic(message, subscription),
            messageType: GetMessageType(message),
            correlationId: GetCorrelationId(message),
            timeStamp: GetTimestamp(message),
            type: new CloudEventsType(message.NMSType),
            contentType: GetContentType(message),
            source: GetSource(message),
            dataSchema: GetDataSchema(message),
            subject: GetSubject(message),
            replyTo: GetReplyTo(message))
        {
            SpecVersion = GetSpecVersion(message),
            Bag = bag 
        };

        var body = message switch
        {
            IBytesMessage bytesMessage => new MessageBody(bytesMessage.Content),
            ITextMessage textMessage => new MessageBody(textMessage.Text),
            _ => throw new NotSupportedException($"Unsupported message type {message.GetType()}")
        };

        return new Message(header, body);

        static Id GetId(IMessage message)
        {
            return Id.Create(message.Properties.Contains(HeaderNames.Id) ? message.Properties.GetString(HeaderNames.Id) : message.NMSMessageId);
        }

        static RoutingKey GetTopic(IMessage message, ActiveMqSubscription subscription)
        {
            if (!message.Properties.Contains(HeaderNames.Topic))
            {
                return subscription.RoutingKey;
            }
            
            var topic = message.Properties.GetString(HeaderNames.Topic);
            return string.IsNullOrEmpty(topic) ? subscription.RoutingKey : new RoutingKey(topic);
        }

        static MessageType GetMessageType(IMessage message)
        {
            if (!message.Properties.Contains(HeaderNames.MessageType))
            {
                return MessageType.MT_EVENT;
            }
            
            var messageType = message.Properties.GetString(HeaderNames.MessageType);
            return Enum.TryParse(messageType, true, out MessageType result) ? result : MessageType.MT_EVENT;
        }

        static Id GetCorrelationId(IMessage message) 
            => string.IsNullOrEmpty(message.NMSCorrelationID) ? Id.Empty : Id.Create(message.NMSCorrelationID);

        static ContentType GetContentType(IMessage message)
        {
            if (!message.Properties.Contains(HeaderNames.DataContentType))
            {
                return new ContentType("text/plain");
            }
            
            var contentType = message.Properties.GetString(HeaderNames.DataContentType);
            return string.IsNullOrEmpty(contentType) ? new ContentType("text/plain") : new ContentType(contentType);
        }

        static string GetSpecVersion(IMessage message)
        {
            if (!message.Properties.Contains(HeaderNames.SpecVersion))
            {
                return MessageHeader.DefaultSpecVersion;
            }
            
            var specVersion = message.Properties.GetString(HeaderNames.SpecVersion);
            return string.IsNullOrEmpty(specVersion) ? MessageHeader.DefaultSpecVersion : specVersion;

        }

        static Uri GetSource(IMessage message)
        {
            if (!message.Properties.Contains(HeaderNames.Source))
            {
                return new Uri(MessageHeader.DefaultSource, UriKind.RelativeOrAbsolute);
            }
            
            var source = message.Properties.GetString(HeaderNames.Source);
            return string.IsNullOrEmpty(source) ? new Uri(MessageHeader.DefaultSource) : new Uri(source, UriKind.RelativeOrAbsolute);
        }
        
        static Uri? GetDataSchema(IMessage message)
        {
            if (!message.Properties.Contains(HeaderNames.DataSchema))
            {
                return null;
            }
            
            var dataSchema = message.Properties.GetString(HeaderNames.DataSchema);
            return string.IsNullOrEmpty(dataSchema) ? null : new Uri(dataSchema, UriKind.RelativeOrAbsolute);
        }
        
        static string? GetSubject(IMessage message)
        {
            return message.Properties.Contains(HeaderNames.Subject) ? message.Properties.GetString(HeaderNames.Subject) : null;
        }

        static RoutingKey? GetReplyTo(IMessage message) => message.NMSReplyTo == null ? null : new RoutingKey(message.NMSReplyTo.ToString()!);

        static DateTimeOffset GetTimestamp(IMessage message)
        {
            if (message.Properties.Contains(HeaderNames.TimeStamp)
                && DateTimeOffset.TryParse(message.Properties.GetString(HeaderNames.TimeStamp), out var timestamp))
            {
                return timestamp;
            }

            return DateTimeOffset.UtcNow;
        }
    }
    
    /// <summary>
    /// Disposes of the resources held by the consumer (NMS consumer, destination, and session).
    /// </summary>
    public void Dispose()
    {
        _consumer.Dispose();
        _queue.Dispose();
        _session.Dispose();
    }

    /// <summary>
    /// Asynchronously disposes of the resources held by the consumer.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous disposal operation.</returns>
    public ValueTask DisposeAsync()
    {
        Dispose(); 
        return new ValueTask();
    }
}
