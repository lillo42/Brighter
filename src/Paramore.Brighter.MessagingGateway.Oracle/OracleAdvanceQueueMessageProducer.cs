using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Paramore.Brighter.MessagingGateway.Oracle;

/// <summary>
/// Provides message production capabilities for Oracle Advanced Queuing (AQ) system.
/// Implements synchronous and asynchronous message sending patterns with support for
/// delayed delivery, priority handling, and message expiration.
/// </summary>
/// <remarks>
/// <para>
/// This producer handles message serialization to Oracle AQ compatible formats including
/// JSON, XML, raw binary, and user-defined types (UDT). It manages Oracle connection
/// lifecycle and ensures proper message configuration based on publication settings.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
///   <item>Support for delayed message delivery</item>
///   <item>Configurable message priority levels</item>
///   <item>Message expiration handling</item>
///   <item>Multiple payload type support (JSON, XML, Raw, UDT)</item>
///   <item>Transaction-aware message enqueuing</item>
/// </list>
/// </para>
/// </remarks>
/// <param name="connectionProvider">Provider for Oracle database connections</param>
/// <param name="publication">Configuration for message publication parameters</param>
public class OracleAdvanceQueueMessageProducer(IAmARelationalDbConnectionProvider connectionProvider,
    OracleAdvanceQueuePublication publication) : IAmAMessageProducerSync, IAmAMessageProducerAsync
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
        using var conn = (OracleConnection)connectionProvider.GetConnection();
        using var queue = CreateQueue(conn);
        queue.Enqueue(CreateOracleMessage(message, delay ?? TimeSpan.Zero));
    }
    
    /// <inheritdoc />
    public Task SendAsync(Message message, CancellationToken cancellationToken = default) 
        => SendWithDelayAsync(message, TimeSpan.Zero, cancellationToken);

    /// <inheritdoc />
    public async Task SendWithDelayAsync(Message message, TimeSpan? delay, CancellationToken cancellationToken = default)
    {
#if NETFRAMEWORK
        using var conn = (OracleConnection)await connectionProvider.GetConnectionAsync(cancellationToken);
#else
        await using var conn = (OracleConnection)await connectionProvider.GetConnectionAsync(cancellationToken);
#endif
        using var queue = CreateQueue(conn);
        queue.Enqueue(CreateOracleMessage(message, delay ?? TimeSpan.Zero));
    }

    private OracleAQQueue CreateQueue(OracleConnection connection)
    {
        return new OracleAQQueue(publication.Queue, connection)
        {
            MessageType = publication.MessageType, 
            UdtTypeName = publication.UdtTypeName,
            EnqueueOptions = new OracleAQEnqueueOptions
            {
                DeliveryMode = publication.DeliveryMode,
                Visibility = publication.VisibilityMode,
            },
        };
    }

    private OracleAQMessage CreateOracleMessage(Message message, TimeSpan delay)
    {
        return new OracleAQMessage
        {
            Correlation = message.Header.CorrelationId,
            Delay = (int)delay.TotalSeconds,
            Expiration = GetExpiration(message),
            Priority = GetPriority(message),
            Payload = publication.MessageType switch
            {
                OracleAQMessageType.Json or OracleAQMessageType.Xml => new OracleString(message.Body.Value),
                OracleAQMessageType.Raw => new OracleBinary(message.Body.Bytes),
                OracleAQMessageType.Udt => message.Header.Bag[HeaderNames.Payload],
                _ => throw new InvalidOperationException("Unknown message type")
            },
            SenderId = publication.Sender
        };
    }

    private int GetExpiration(Message message)
    {
        if (message.Header.Bag.TryGetValue(HeaderNames.Expiration, out var tmp)
            && tmp is int expiration)
        {
            message.Header.Bag.Remove(HeaderNames.Expiration);
            return expiration;
        }

        return publication.Expiration;
    }

    private int GetPriority(Message message)
    {
        if (message.Header.Bag.TryGetValue(HeaderNames.Priority, out var tmp)
            && tmp is int priority)
        {
            message.Header.Bag.Remove(HeaderNames.Priority);
            return priority;
        }

        return publication.Priority;
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        Span?.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Span?.Dispose();
        return new ValueTask();
    }

    
}
