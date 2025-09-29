using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public class ActiveMqMessageConsumer : IAmAMessageConsumerSync, IAmAMessageConsumerAsync
{
    private readonly ActiveMqSubscription _subscription;
    private readonly ISession _session;
    private readonly IDestination _queue;
    private readonly IMessageConsumer _consumer;

    public ActiveMqMessageConsumer(IConnection connection, ActiveMqSubscription subscription)
    {
        _subscription = subscription;
        
        _session = connection.CreateSession(AcknowledgementMode.Transactional);
        _queue = CreateDestination(_session, _subscription);
        _consumer = CreateConsumer(_session, _queue, _subscription);
    }

    /// <inheritdoc />
    public void Acknowledge(Message message)
    {
        _session.Commit();
    }
    
    /// <inheritdoc />
    public async Task AcknowledgeAsync(Message message, CancellationToken cancellationToken = default)
    {
        await _session.CommitAsync();
    }

    /// <inheritdoc />
    public bool Reject(Message message)
    {
        _session.Commit();
        return true;
    }
    
    /// <inheritdoc />
    public async Task<bool> RejectAsync(Message message, CancellationToken cancellationToken = default)
    {
        await _session.CommitAsync();
        return true;
    }

    /// <inheritdoc />
    public void Purge()
    {
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
    
    /// <inheritdoc />
    public async Task PurgeAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var message = await _consumer.ReceiveAsync(TimeSpan.FromSeconds(1));
            if (message == null)
            {
                break;
            }
            
            await _session.CommitAsync();
        }
    }

    /// <inheritdoc />
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
            return [new Message()];
        }
    }

    private static IDestination CreateDestination(ISession session, ActiveMqSubscription subscription)
    {
        return subscription switch
        {
            ActiveMqQueueSubscription => session.GetQueue(subscription.ChannelName.Value),
            ActiveMqTopicSubscription => session.GetTopic(subscription.RoutingKey.Value),
            _ => throw new InvalidOperationException("Invalid ActiveMqSubscription")
        };
    }

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

    /// <inheritdoc />
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
            return [new Message()];
        }
    }

    /// <inheritdoc />
    public bool Requeue(Message message, TimeSpan? delay = null)
    {
        _session.Rollback();
        return true;
    }
    
    /// <inheritdoc />
    public async Task<bool> RequeueAsync(Message message, TimeSpan? delay = null,
        CancellationToken cancellationToken = default)
    {
        await _session.RollbackAsync();
        return true;
    }

    private static Message ToBrighterMessage(IMessage message, ActiveMqSubscription subscription)
    {
        var bag = new Dictionary<string, object>();
        foreach (var key in message.Properties.Keys)
        {
            if (key is not string keyString)
            {
                continue;
            }

            bag[keyString] = message.Properties[keyString];
        }

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
            if (!message.Properties.Contains(HeaderNames.DataContentType))
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
            return message.Properties.Contains(HeaderNames.DataContentType) ? message.Properties.GetString(HeaderNames.Subject) : null;
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
    
    /// <inheritdoc />
    public void Dispose()
    {
        _consumer.Dispose();
        _queue.Dispose();
        _session.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose(); 
        return new ValueTask();
    }
}
