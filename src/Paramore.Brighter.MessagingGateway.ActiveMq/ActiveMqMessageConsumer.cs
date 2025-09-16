using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public class ActiveMqMessageConsumer(IConnection connection, ActiveMqSubscription subscription)
    : IAmAMessageConsumerSync, IAmAMessageConsumerAsync
{
    /// <inheritdoc />
    public void Acknowledge(Message message)
    {
        if (!message.Header.Bag.TryGetValue(HeaderNames.ReceiptHandle, out object? value) 
            || value is not IMessage activeMessage)
        {
            return;
        }

        activeMessage.Acknowledge();
    }
    
    /// <inheritdoc />
    public async Task AcknowledgeAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (!message.Header.Bag.TryGetValue(HeaderNames.ReceiptHandle, out object? value) 
            || value is not IMessage activeMessage)
        {
            return;
        }

        await activeMessage.AcknowledgeAsync();
    }

    /// <inheritdoc />
    public bool Reject(Message message)
    {
        if (!message.Header.Bag.TryGetValue(HeaderNames.ReceiptHandle, out object? value) 
            || value is not IMessage activeMessage)
        {
            return true;
        }

        activeMessage.Acknowledge();
        return true;
    }
    
    /// <inheritdoc />
    public async Task<bool> RejectAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (!message.Header.Bag.TryGetValue(HeaderNames.ReceiptHandle, out object? value) 
         || value is not IMessage activeMessage)
        {
            return true;
        }

        await activeMessage.AcknowledgeAsync();
        return true;
    }

    /// <inheritdoc />
    public void Purge()
    {
        using var session = connection.CreateSession();
        using var queue = CreateDestination(session, subscription);
        using var consumer = CreateConsumer(session, queue, subscription);
        
        while (true)
        {
            var message = consumer.Receive(TimeSpan.FromSeconds(1));
            if (message == null)
            {
                break;
            }
            
            message.Acknowledge();
        }
    }
    
    /// <inheritdoc />
    public async Task PurgeAsync(CancellationToken cancellationToken = default)
    {
        using var session = await connection.CreateSessionAsync();
        using var queue = await CreateDestinationAsync(session, subscription);
        using var consumer = await CreateConsumerAsync(session, queue, subscription);
        
        while (true)
        {
            var message = await consumer.ReceiveAsync(TimeSpan.FromSeconds(1));
            if (message == null)
            {
                break;
            }
            
            await message.AcknowledgeAsync();
        }
    }

    /// <inheritdoc />
    public Message[] Receive(TimeSpan? timeOut = null)
    {
        try
        {
            using var session = connection.CreateSession();
            using var queue = CreateDestination(session, subscription);
            using var consumer = CreateConsumer(session, queue, subscription);
            
            var message = consumer.Receive(timeOut ?? TimeSpan.Zero);
            if (message == null)
            {
                return [new Message()];
            }

            return [ToBrighterMessage(message, subscription)];
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
            using var session = await connection.CreateSessionAsync(AcknowledgementMode.ClientAcknowledge);
            using var queue = await CreateDestinationAsync(session, subscription);
            using var consumer = await CreateConsumerAsync(session, queue, subscription);
            
            var message = await consumer.ReceiveAsync(timeOut ?? TimeSpan.Zero);
            if (message == null)
            {
                return [new Message()];
            }

            return [ToBrighterMessage(message, subscription)];
        }
        catch
        {
            return [new Message()];
        }
    }
    
    private static async Task<IDestination> CreateDestinationAsync(ISession session, ActiveMqSubscription subscription)
    {
        return subscription switch
        {
            ActiveMqQueueSubscription => await session.GetQueueAsync(subscription.ChannelName.Value),
            ActiveMqTopicSubscription => await session.GetTopicAsync(subscription.RoutingKey.Value),
            _ => throw new InvalidOperationException("Invalid ActiveMqSubscription")
        };
    }

    private static async Task<IMessageConsumer> CreateConsumerAsync(ISession session,
        IDestination destination,
        ActiveMqSubscription subscription)
    {
        if (subscription is ActiveMqQueueSubscription)
        {
            return await session.CreateConsumerAsync(destination, subscription.Selector, subscription.NoLocal);
        }
        
        if(subscription is ActiveMqTopicSubscription topicSubscription)
        {
            return topicSubscription.ConsumerType switch
            {
                ConsumerType.Default => await session.CreateConsumerAsync(destination, subscription.Selector, subscription.NoLocal),
                ConsumerType.Durable => await session.CreateDurableConsumerAsync((ITopic)destination, subscription.Name, subscription.Selector, subscription.NoLocal),
                ConsumerType.Share => await session.CreateSharedConsumerAsync((ITopic)destination, subscription.Name, subscription.Selector),
                ConsumerType.ShareDurable => await session.CreateSharedDurableConsumerAsync((ITopic)destination, subscription.Name, subscription.Selector),
                _ => throw new ConfigurationException("Invalid consumer type")
            };
        }

        throw new ConfigurationException("Invalid ActiveMqSubscription");
    }

    /// <inheritdoc />
    public bool Requeue(Message message, TimeSpan? delay = null)
    {
        // ActiveMQ doesn't support requeue
        // we need to wait ActiveMQ send the message again
        return true;
    }
    
    /// <inheritdoc />
    public Task<bool> RequeueAsync(Message message, TimeSpan? delay = null,
        CancellationToken cancellationToken = default)
    {
        // ActiveMQ doesn't support requeue
        // we need to wait ActiveMQ send the message again
        return Task.FromResult(true);
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
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return new ValueTask();
    }
}
