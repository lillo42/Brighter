using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public class ActiveMqMessageProducer(IConnection connection, 
    ActiveMqPublication publication,
    IDestination destination,
    TimeProvider timeProvider) : IAmAMessageProducerSync, IAmAMessageProducerAsync
{
    /// <inheritdoc />
    public Publication Publication => publication;
    
    /// <inheritdoc />
    public Activity? Span { get; set; }
    
    /// <inheritdoc />
    public IAmAMessageScheduler? Scheduler { get; set; }

    /// <inheritdoc />
    public void Send(Message message) => SendWithDelay(message, TimeSpan.Zero);

    /// <inheritdoc />
    public void SendWithDelay(Message message, TimeSpan? delay)
    {
        using var session = connection.CreateSession();
        using var producer = session.CreateProducer(destination);
        
        var activeMqMessage = session.CreateBytesMessage(message.Body.Bytes);
        AddProperties(activeMqMessage, message, delay ?? TimeSpan.Zero);
        
        producer.Send(activeMqMessage, publication.DeliveryMode, publication.Priority, publication.TimeToLive);
    }
    
    /// <inheritdoc />
    public Task SendAsync(Message message, CancellationToken cancellationToken = default) 
        => SendWithDelayAsync(message, TimeSpan.Zero, cancellationToken);

    /// <inheritdoc />
    public async Task SendWithDelayAsync(Message message, TimeSpan? delay, CancellationToken cancellationToken = default)
    {
        using var session = await connection.CreateSessionAsync();
        using var producer = await session.CreateProducerAsync(destination);
        
        var activeMqMessage = await session.CreateBytesMessageAsync(message.Body.Bytes);
        AddProperties(activeMqMessage, message, delay ?? TimeSpan.Zero);
        
        await producer.SendAsync(activeMqMessage, publication.DeliveryMode, publication.Priority, publication.TimeToLive);
    }

    private void AddProperties(IMessage activeMessage, Message message, TimeSpan delay)
    {
        activeMessage.NMSMessageId = message.Id;
        activeMessage.NMSCorrelationID = message.Header.CorrelationId;
        activeMessage.NMSTimestamp = message.Header.TimeStamp.DateTime;
        activeMessage.NMSType = message.Header.Type;
        
        // if (!RoutingKey.IsNullOrEmpty(message.Header.ReplyTo))
        // {
        //     activeMessage.NMSReplyTo = new ActiveMQQueue(message.Header.ReplyTo.Value);
        // }

        if (delay != TimeSpan.Zero)
        {
            activeMessage.NMSDeliveryTime = (timeProvider.GetUtcNow() + delay).DateTime;
        }
        
        activeMessage.Properties[HeaderNames.Topic] = message.Header.Topic.Value;
        activeMessage.Properties[HeaderNames.MessageType] = message.Header.MessageType.ToString();
        activeMessage.Properties[HeaderNames.DataContentType] = message.Header.ContentType.ToString();
        activeMessage.Properties[HeaderNames.SpecVersion] = message.Header.SpecVersion;
        activeMessage.Properties[HeaderNames.Source] = message.Header.Source;

        if (message.Header.DataSchema != null)
        {
            activeMessage.Properties[HeaderNames.DataSchema] = message.Header.DataSchema.ToString();
        }

        if (!string.IsNullOrEmpty(message.Header.Subject))
        {
            activeMessage.Properties[HeaderNames.Subject] = message.Header.Subject;
        }
        
        foreach (var keyPair in message.Header.Bag)
        {
            activeMessage.Properties[keyPair.Key] = keyPair.Value.ToString();
        }
    }
    
    /// <inheritdoc />
    public void Dispose() => Span?.Dispose();

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return new ValueTask();
    }
}
