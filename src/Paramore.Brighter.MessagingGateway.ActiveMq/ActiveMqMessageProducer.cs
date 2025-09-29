using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.ActiveMQ.Commands;
using OpenTelemetry;
using Paramore.Brighter.Extensions;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

/// <summary>
/// Implements a message producer for ActiveMQ (using the NMS client) that is compliant
/// with Brighter's synchronous and asynchronous producer interfaces.
/// </summary>
/// <remarks>
/// This producer handles the conversion of a Brighter <see cref="Message"/> to an NMS <see cref="IMessage"/>,
/// including setting message properties, correlation IDs, timestamps, and handling message destinations (Queues/Topics).
/// </remarks>
public class ActiveMqMessageProducer(IConnection connection, ActiveMqPublication publication, TimeProvider timeProvider) 
    : IAmAMessageProducerSync, IAmAMessageProducerAsync, IAmABulkMessageProducerAsync
{
    /// <summary>
    /// Gets the publication details used by this producer, which includes ActiveMQ-specific settings.
    /// </summary>
    /// <value>
    /// The <see cref="ActiveMqPublication"/> configuration instance.
    /// </value>
    public Publication Publication => publication;
    
    /// <summary>
    /// Gets or sets the current distributed tracing span associated with the message being sent.
    /// </summary>
    /// <value>
    /// An <see cref="Activity"/> instance, or <see langword="null"/>.
    /// </value>
    public Activity? Span { get; set; }
    
    /// <summary>
    /// Gets or sets a scheduler instance for delaying messages.
    /// </summary>
    /// <value>
    /// An <see cref="IAmAMessageScheduler"/> instance, or <see langword="null"/>.
    /// </value>
    public IAmAMessageScheduler? Scheduler { get; set; }

    /// <summary>
    /// Sends a message to the ActiveMQ broker synchronously without any delay.
    /// </summary>
    /// <param name="message">The message to send.</param>
    public void Send(Message message)
    {
        SendWithDelay(message, TimeSpan.Zero);
    }

    /// <summary>
    /// Sends a message to the ActiveMQ broker synchronously, with an optional delay.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="delay">The time span to delay message delivery. If <see cref="TimeSpan.Zero"/> or <see langword="null"/>, the message is sent immediately.</param>
    public void SendWithDelay(Message message, TimeSpan? delay)
    {
        // Use the connection provided in the constructor
        using var session = connection.CreateSession();
        using var destination = GetDestination(session);
        using var producer = session.CreateProducer(destination);
        
        var activeMqMessage = session.CreateBytesMessage(message.Body.Bytes);
        AddProperties(activeMqMessage, message, delay ?? TimeSpan.Zero);
        
        producer.Send(activeMqMessage, publication.DeliveryMode, publication.Priority, publication.TimeToLive);
    }
    
    /// <summary>
    /// Determines the correct ActiveMQ destination type (Queue or Topic) based on the publication type.
    /// </summary>
    /// <param name="session">The current NMS session.</param>
    /// <returns>An NMS <see cref="IDestination"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="Publication"/> type is not a known ActiveMQ type.</exception>
    private IDestination GetDestination(ISession session) => publication switch
    {
        ActiveMqQueuePublication => session.GetQueue(publication.Topic!.Value),
        ActiveMqTopicPublication => session.GetTopic(publication.Topic!.Value),
        _ => throw new InvalidOperationException("Invalid publication")
    };
    
    /// <summary>
    /// Sends a message to the ActiveMQ broker asynchronously without any delay.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    public Task SendAsync(Message message, CancellationToken cancellationToken = default)
    {
        return SendWithDelayAsync(message, TimeSpan.Zero, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the ActiveMQ broker asynchronously, with an optional delay.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="delay">The time span to delay message delivery. If <see cref="TimeSpan.Zero"/> or <see langword="null"/>, the message is sent immediately.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    public async Task SendWithDelayAsync(Message message, TimeSpan? delay, CancellationToken cancellationToken = default)
    {
        using var session = await connection.CreateSessionAsync();
        using var destination = await GetDestinationAsync(session);
        using var producer = await session.CreateProducerAsync(destination);
        
        var activeMqMessage = await session.CreateBytesMessageAsync(message.Body.Bytes);
        AddProperties(activeMqMessage, message, delay ?? TimeSpan.Zero);
        
        await producer.SendAsync(activeMqMessage, publication.DeliveryMode, publication.Priority, publication.TimeToLive);
    }
    
    /// <summary>
    /// Asynchronously determines the correct ActiveMQ destination type (Queue or Topic) based on the publication type.
    /// </summary>
    /// <param name="session">The current NMS session.</param>
    /// <returns>A task that returns an NMS <see cref="IDestination"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="Publication"/> type is not a known ActiveMQ type.</exception>
    private async Task<IDestination> GetDestinationAsync(ISession session) => publication switch
    {
        ActiveMqQueuePublication => await session.GetQueueAsync(publication.Topic!.Value),
        ActiveMqTopicPublication => await session.GetTopicAsync(publication.Topic!.Value),
        _ => throw new InvalidOperationException("Invalid publication")
    };

    /// <summary>
    /// Maps the Brighter message header properties to the NMS message properties and standard NMS headers.
    /// </summary>
    /// <param name="activeMessage">The NMS message instance to populate.</param>
    /// <param name="message">The Brighter message instance containing the header and body.</param>
    /// <param name="delay">The message delay, used to set the <see cref="P:Apache.NMS.IMessage.NMSDeliveryTime"/> property.</param>
    private void AddProperties(IMessage activeMessage, Message message, TimeSpan delay)
    {
        // Set standard NMS headers from Brighter headers
        activeMessage.NMSCorrelationID = message.Header.CorrelationId;
        activeMessage.NMSTimestamp = message.Header.TimeStamp.DateTime;
        activeMessage.NMSType = message.Header.Type;
        
        // Handle ReplyTo destination type (Queue or Topic)
        if (!RoutingKey.IsNullOrEmpty(message.Header.ReplyTo))
        {
            if (message.Header.Bag.TryGetValue(HeaderNames.ReplyToType, out var tmp) 
                && tmp is ReplyToType.Topic)
            {
                activeMessage.NMSReplyTo = new ActiveMQTopic(message.Header.ReplyTo.Value);
            }
            else
            {
                activeMessage.NMSReplyTo = new ActiveMQQueue(message.Header.ReplyTo.Value);
            }
        }

        // Handle scheduled delivery delay
        if (delay != TimeSpan.Zero)
        {
            activeMessage.NMSDeliveryTime = (timeProvider.GetUtcNow() + delay).DateTime;
        }
        
        // Map Brighter/CloudEvents headers to NMS message properties
        activeMessage.Properties[HeaderNames.Id] = message.Header.MessageId.Value;
        activeMessage.Properties[HeaderNames.Topic] = message.Header.Topic.Value;
        activeMessage.Properties[HeaderNames.TimeStamp] = message.Header.TimeStamp.ToRfc3339();
        activeMessage.Properties[HeaderNames.MessageType] = message.Header.MessageType.ToString();
        activeMessage.Properties[HeaderNames.DataContentType] = message.Header.ContentType.ToString();
        activeMessage.Properties[HeaderNames.SpecVersion] = message.Header.SpecVersion;
        activeMessage.Properties[HeaderNames.Source] = message.Header.Source.ToString();

        if (message.Header.DataSchema != null)
        {
            activeMessage.Properties[HeaderNames.DataSchema] = message.Header.DataSchema.ToString();
        }

        if (!string.IsNullOrEmpty(message.Header.Subject))
        {
            activeMessage.Properties[HeaderNames.Subject] = message.Header.Subject;
        }
        
        // Copy remaining Brighter message bag headers to NMS properties
        foreach (var keyPair in message.Header.Bag)
        {
            activeMessage.Properties[keyPair.Key] = keyPair.Value.ToString();
        }
    }
    
    /// <summary>
    /// Disposes of the resources used by the producer, specifically the tracing span.
    /// </summary>
    public void Dispose()
    {
        Span?.Dispose();
    }

    /// <summary>
    /// Asynchronously disposes of the resources used by the producer.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous disposal operation.</returns>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return new ValueTask();
    }

    /// <summary>
    /// Creates a collection of message batches from a stream of messages.
    /// For ActiveMQ, all messages are placed into a single batch, as ActiveMQ transactional sending
    /// handles atomicity at the session/commit level rather than requiring broker-specific batching objects.
    /// </summary>
    /// <param name="messages">The stream of <see cref="Message"/> objects to be batched.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that returns an enumerable collection containing a single 
    /// <see cref="IAmAMessageBatch"/> with all provided messages.
    /// </returns>
    public ValueTask<IEnumerable<IAmAMessageBatch>> CreateBatchesAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
    {
        return new ValueTask<IEnumerable<IAmAMessageBatch>>([new MessageBatch(messages)]);
    }
    
    /// <summary>
    /// Sends a batch of messages to the ActiveMQ broker asynchronously within a single transaction.
    /// All messages in the batch are sent and committed atomically.
    /// </summary>
    /// <param name="batch">The batch of messages to send, which must be a <see cref="MessageBatch"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous batch send and commit operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the provided batch is not an instance of <see cref="MessageBatch"/>.
    /// </exception>
    public async Task SendAsync(IAmAMessageBatch batch, CancellationToken cancellationToken)
    {
        if (batch is not MessageBatch messageBatch)
        {
            throw new InvalidOperationException("Invalid batch");
        }
        
        using var session = await connection.CreateSessionAsync(AcknowledgementMode.Transactional);
        using var destination = await GetDestinationAsync(session);
        using var producer = await session.CreateProducerAsync(destination);

        foreach (var message in messageBatch.Content)
        {
            var activeMqMessage = await session.CreateBytesMessageAsync(message.Body.Bytes);
            AddProperties(activeMqMessage, message, TimeSpan.Zero);
            await producer.SendAsync(activeMqMessage, publication.DeliveryMode, publication.Priority, publication.TimeToLive);
        }

        await session.CommitAsync();
    }
}
