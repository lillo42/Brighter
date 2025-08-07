using Oracle.ManagedDataAccess.Client;

namespace Paramore.Brighter.MessagingGateway.Oracle;

/// <summary>
/// Represents the configuration for publishing messages to an Oracle Advanced Queue (AQ)
/// </summary>
/// <remarks>
/// <para>
/// This class contains Oracle-specific settings for message publication, including payload type,
/// delivery guarantees, visibility behavior, and message metadata. It extends the base Publication
/// class with Oracle AQ-specific capabilities.
/// </para>
/// <para>
/// Default configuration:
/// <list type="bullet">
///   <item>MessageType: Raw (binary payload)</item>
///   <item>DeliveryMode: Persistent (guaranteed delivery)</item>
///   <item>VisibilityMode: Immediate (visible upon enqueue)</item>
///   <item>Priority: 5 (medium priority)</item>
/// </list>
/// </para>
/// </remarks>
public class OracleAdvanceQueuePublication : Publication
{
    public string Queue { get; set; } = string.Empty;
    
    /// <summary>
    /// Specifies the serialization format for message payloads
    /// </summary>
    /// <value>
    /// Default: OracleAQMessageType.Raw
    /// </value>
    /// <remarks>
    /// Determines how message bodies will be serialized and stored in Oracle:
    /// <list type="bullet">
    ///   <item>Raw: Binary payload (byte[])</item>
    ///   <item>Json: JSON-formatted string</item>
    ///   <item>Xml: XML-formatted string</item>
    ///   <item>Udt: User-Defined Type (requires UdtTypeName)</item>
    /// </list>
    /// </remarks>
    public OracleAQMessageType MessageType { get; set; } = OracleAQMessageType.Raw;
    
    /// <summary>
    /// Determines message persistence behavior
    /// </summary>
    /// <value>
    /// Default: OracleAQMessageDeliveryMode.Persistent
    /// </value>
    /// <remarks>
    /// <para>
    /// Persistent: Messages survive database restarts (stored in queue tables)
    /// </para>
    /// <para>
    /// Buffered: Higher performance but messages not recoverable after instance failure
    /// </para>
    /// </remarks>
    public OracleAQMessageDeliveryMode DeliveryMode { get; set; } = OracleAQMessageDeliveryMode.Persistent;
    
    /// <summary>
    /// Controls when messages become visible to consumers
    /// </summary>
    /// <value>
    /// Default: OracleAQVisibilityMode.OnCommit
    /// </value>
    /// <remarks>
    /// <para>
    /// Immediate: Visible immediately after enqueue (non-transactional)
    /// </para>
    /// <para>
    /// OnCommit: Visible only after transaction commit (recommended for reliability)
    /// </para>
    /// </remarks>
    public OracleAQVisibilityMode VisibilityMode { get; set; } = OracleAQVisibilityMode.OnCommit;
    
    /// <summary>
    /// Oracle UDT (User-Defined Type) name for structured payloads
    /// </summary>
    /// <remarks>
    /// Required when MessageType = OracleAQMessageType.Udt. Must match a valid
    /// Oracle object type in the target schema. Format: [SCHEMA.]TYPE_NAME
    /// </remarks>
    public string? UdtTypeName { get; set; }
    
    /// <summary>
    /// Message Time-To-Live (TTL) in seconds
    /// </summary>
    /// <value>
    /// Default: 0 (no expiration)
    /// </value>
    /// <remarks>
    /// After expiration period, messages are automatically moved to exception queue
    /// or deleted based on queue configuration
    /// </remarks>
    public int Expiration { get; set; }
    
    /// <summary>
    /// Message processing priority (0-10 scale)
    /// </summary>
    /// <value>
    /// Default: 5 (medium priority)
    /// </value>
    /// <remarks>
    /// Lower values = higher priority. Affects dequeue order when using priority queues.
    /// </remarks>
    public int Priority { get; set; } = 5;
    
    /// <summary>
    /// Sender identification metadata
    /// </summary>
    /// <remarks>
    /// Optional agent information identifying the message sender.
    /// Appears as SENDER_NAME and SENDER_ADDRESS in queue tables.
    /// </remarks>
    public OracleAQAgent? Sender { get; set; }
    
    public QueueAttribute? Attribute { get; set; }
}


/// <summary>
/// Generic version of Oracle Advanced Queue publication configuration
/// </summary>
/// <typeparam name="TMessage">
/// The specific message type this publication configuration applies to.
/// Must inherit from <see cref="Message"/>
/// </typeparam>
/// <remarks>
/// <para>
/// Associates publication settings with a specific message type. Automatically sets
/// the RequestType property to the generic type parameter.
/// </para>
/// <para>
/// Use this class for strong-typed message configuration in dependency injection systems.
/// </para>
/// </remarks>
public class OracleAdvanceQueuePublication<TMessage> : OracleAdvanceQueuePublication
    where TMessage : Message 
{
    /// <summary>
    /// Initializes a new instance of the publication configuration
    /// </summary>
    /// <remarks>
    /// Sets the RequestType property to typeof(TMessage) for message type association
    /// </remarks>
    public OracleAdvanceQueuePublication()
    {
        RequestType = typeof(TMessage);
    }
}
