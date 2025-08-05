using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public class ActiveMqMessagePullConsumer : IAmAMessageConsumerSync, IAmAMessageConsumerAsync
{
    private readonly ISession _session;
    private readonly IMessageConsumer _consumer;
    private readonly RoutingKey _defaultTopic;

    public ActiveMqMessagePullConsumer(IConnection connection, IDestination destination, RoutingKey defaultTopic)
    {
        _defaultTopic = defaultTopic;
        _session = connection.CreateSession();
        _consumer = _session.CreateConsumer(destination);
    }

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
        while (true)
        {
            var message = _consumer.Receive(TimeSpan.FromSeconds(1));
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
        while (true)
        {
            var message = await _consumer.ReceiveAsync(TimeSpan.FromSeconds(1));
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
            var message = _consumer.Receive(timeOut ?? TimeSpan.Zero);
            if (message == null)
            {
                return [new Message()];
            }

            return [ToBrighterMessage(message)];
        }
        catch
        {
            return [new Message()];
        }
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

            return [ToBrighterMessage(message)];
        }
        catch
        {
            return [new Message()];
        }
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

    private Message ToBrighterMessage(IMessage message)
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
            messageId: Id.Create(message.NMSMessageId),
            topic: GetTopic(),
            messageType: GetMessageType(),
            correlationId: GetCorrelationId(),
            timeStamp: message.NMSDeliveryTime,
            type: message.NMSType,
            contentType: GetContentType(),
            source: GetSource(),
            dataSchema: GetDataSchema(),
            subject: GetSubject(),
            replyTo: GetReplyTo())
        {
            SpecVersion = GetSpecVersion(),
            Bag = bag 
        };

        var body = message switch
        {
            IBytesMessage bytesMessage => new MessageBody(bytesMessage.Content),
            ITextMessage textMessage => new MessageBody(textMessage.Text),
            _ => throw new NotSupportedException($"Unsupported message type {message.GetType()}")
        };

        return new Message(header, body);

        RoutingKey GetTopic()
        {
            var topic = message.Properties.GetString(HeaderNames.Topic);
            return string.IsNullOrEmpty(topic) ? _defaultTopic : new RoutingKey(topic);
        }

        MessageType GetMessageType()
        {
            var messageType = message.Properties.GetString(HeaderNames.MessageType);
            return Enum.TryParse(messageType, true, out MessageType result) ? result : MessageType.MT_EVENT;
        }

        Id GetCorrelationId() 
            => string.IsNullOrEmpty(message.NMSCorrelationID) ? Id.Empty : Id.Create(message.NMSCorrelationID);

        ContentType GetContentType()
        {
            var contentType = message.Properties.GetString(HeaderNames.DataContentType);
            return string.IsNullOrEmpty(contentType) ? new ContentType("text/plain") : new ContentType(contentType);
        }

        string GetSpecVersion()
        {
            var specVersion = message.Properties.GetString(HeaderNames.SpecVersion);
            return string.IsNullOrEmpty(specVersion) ? HeaderNames.SpecVersion : specVersion;
        }

        Uri GetSource()
        {
            var source = message.Properties.GetString(HeaderNames.Source);
            return string.IsNullOrEmpty(source) ? new Uri(MessageHeader.DefaultSource) : new Uri(source);
        }
        
        Uri? GetDataSchema()
        {
            var dataSchema = message.Properties.GetString(HeaderNames.DataSchema);
            return string.IsNullOrEmpty(dataSchema) ? null : new Uri(dataSchema);
        }
        
        string? GetSubject() => message.Properties.GetString(HeaderNames.Subject);

        RoutingKey? GetReplyTo() => message.NMSReplyTo == null ? null : new RoutingKey(message.NMSReplyTo.ToString()!);
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        _session.Dispose();
        _consumer.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(_session);
        await CastAndDispose(_consumer);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }
}
