using System;
using System.Text;
using Oracle.ManagedDataAccess.Client;

namespace Paramore.Brighter.MessagingGateway.Oracle;

/// <summary>
/// Represents a subscription configuration for consuming messages from Oracle Advanced Queuing (AQ)
/// </summary>
/// <remarks>
/// <para>
/// This class extends the base Subscription with Oracle AQ-specific consumption settings including
/// dequeue behavior, visibility modes, and message type handling. It provides configuration for
/// reliable message processing in Oracle AQ environments.
/// </para>
/// <para>
/// Default configuration:
/// <list type="bullet">
///   <item>MessageType: Raw (binary payload)</item>
///   <item>DequeueMode: Locked (transactional processing)</item>
///   <item>Visibility: OnCommit (transaction-safe)</item>
///   <item>Encoding: UTF-8</item>
///   <item>Timeout: 0 (no wait)</item>
/// </list>
/// </para>
/// </remarks>
public class OracleAdvanceQueueSubscription : Subscription
{
    /// <summary>
    /// Specifies the expected payload format for messages
    /// </summary>
    /// <remarks>
    /// Determines how message payloads will be deserialized:
    /// <list type="bullet">
    ///   <item>Raw: Binary payload (byte[])</item>
    ///   <item>Json: JSON-formatted string</item>
    ///   <item>Xml: XML-formatted string</item>
    ///   <item>Udt: User-Defined Type (requires custom mapping)</item>
    /// </list>
    /// Must match the publisher's message type configuration.
    /// </remarks>
    public OracleAQMessageType MessageType { get; } 
    
    /// <summary>
    /// Determines how messages are locked during dequeue
    /// </summary>
    /// <remarks>
    /// <para>
    /// Locked: Message is locked until commit/rollback (default, transactional)
    /// </para>
    /// <para>
    /// Remove: Immediately deletes message on dequeue
    /// </para>
    /// <para>
    /// Browse: Read without locking or removing
    /// </para>
    /// </remarks>
    public OracleAQDequeueMode  DequeueMode { get; }
    
    /// <summary>
    /// Controls when dequeued messages become visible to other consumers
    /// </summary>
    /// <remarks>
    /// <para>
    /// OnCommit: Message remains hidden until transaction commit (recommended)
    /// </para>
    /// <para>
    /// Immediate: Changes visible immediately (non-transactional)
    /// </para>
    /// </remarks>
    public OracleAQVisibilityMode  Visibility { get; }
    
    /// <summary>
    /// Name of the consumer for multi-consumer queues
    /// </summary>
    /// <remarks>
    /// Required for multi-consumer queue configurations. Identifies this specific
    /// consumer instance in shared queue scenarios.
    /// </remarks>
    public string? ConsumerName { get; internal set; }
    
    /// <summary>
    /// Character encoding for text-based payloads
    /// </summary>
    /// <remarks>
    /// Default: UTF-8. Used for decoding:
    /// <list type="bullet">
    ///   <item>Json messages</item>
    ///   <item>Xml messages</item>
    ///   <item>String conversion of Raw payloads</item>
    /// </list>
    /// </remarks>
    public Encoding  Encoding { get; internal set; } 
    
    /// <summary>
    /// Character encoding for text-based payloads
    /// </summary>
    /// <remarks>
    /// Default: UTF-8. Used for decoding:
    /// <list type="bullet">
    ///   <item>Json messages</item>
    ///   <item>Xml messages</item>
    ///   <item>String conversion of Raw payloads</item>
    /// </list>
    /// </remarks>
    public OracleAdvanceQueueSubscription(Type dataType, SubscriptionName? subscriptionName = null, 
        ChannelName? channelName = null, RoutingKey? routingKey = null, int bufferSize = 1, int noOfPerformers = 1,
        TimeSpan? timeOut = null, int requeueCount = -1, TimeSpan? requeueDelay = null, int unacceptableMessageLimit = 0, 
        MessagePumpType messagePumpType = MessagePumpType.Reactor, IAmAChannelFactory? channelFactory = null, 
        OnMissingChannel makeChannels = OnMissingChannel.Create, TimeSpan? emptyChannelDelay = null, 
        TimeSpan? channelFailureDelay = null,
        OracleAQMessageType messageType = OracleAQMessageType.Raw,
        OracleAQVisibilityMode visibility = OracleAQVisibilityMode.OnCommit,
        OracleAQDequeueMode dequeueMode = OracleAQDequeueMode.Locked,
        string? consumerName = null,
        Encoding? encoding = null) 
        : base(dataType, subscriptionName, channelName, routingKey, bufferSize, noOfPerformers, timeOut, requeueCount, 
            requeueDelay, unacceptableMessageLimit, messagePumpType, channelFactory, makeChannels, emptyChannelDelay, 
            channelFailureDelay)
    {
        MessageType = messageType;
        DequeueMode = dequeueMode;
        Visibility = visibility;
        ConsumerName = consumerName;
        Encoding = encoding ?? Encoding.UTF8;
    }
}

/// <summary>
/// Generic version of Oracle Advanced Queue subscription configuration
/// </summary>
/// <typeparam name="TMessage">
/// The specific message type this subscription handles, must inherit from <see cref="Message"/>
/// </typeparam>
/// <remarks>
/// <para>
/// Provides strong-typed configuration for message consumers. Automatically sets the dataType
/// parameter to typeof(TMessage) for type-safe dependency injection and handler registration.
/// </para>
/// <para>
/// Recommended for most implementations to ensure proper message type association.
/// </para>
/// </remarks>
public class OracleAdvanceQueueSubscription<TMessage> : OracleAdvanceQueueSubscription
    where TMessage : Message
{
    /// <summary>
    /// Initializes a new strong-typed Oracle AQ subscription
    /// </summary>
    /// <param name="subscriptionName">Unique name for this subscription</param>
    /// <param name="channelName">Channel name for message routing</param>
    /// <param name="routingKey">Routing key for topic-based queues</param>
    /// <param name="bufferSize">Internal buffer size for message prefetch</param>
    /// <param name="noOfPerformers">Number of concurrent consumer threads</param>
    /// <param name="timeOut">Maximum time to wait for messages (null = infinite)</param>
    /// <param name="requeueCount">Max requeue attempts before moving to DLQ (-1 = infinite)</param>
    /// <param name="requeueDelay">Delay before requeueing failed messages</param>
    /// <param name="unacceptableMessageLimit">Max consecutive processing failures before pausing</param>
    /// <param name="messagePumpType">Message processing strategy (Reactor or Task)</param>
    /// <param name="channelFactory">Channel factory implementation</param>
    /// <param name="makeChannels">Channel creation policy when missing</param>
    /// <param name="emptyChannelDelay">Delay when no messages available</param>
    /// <param name="channelFailureDelay">Delay after channel failure</param>
    /// <param name="messageType">Oracle AQ payload format</param>
    /// <param name="visibility">Message visibility behavior</param>
    /// <param name="dequeueMode">Message locking strategy</param>
    /// <param name="consumerName">Consumer identifier for multi-consumer queues</param>
    /// <param name="encoding">Text encoding for message payloads</param>
    public OracleAdvanceQueueSubscription(SubscriptionName? subscriptionName = null, ChannelName? channelName = null, 
        RoutingKey? routingKey = null, int bufferSize = 1, int noOfPerformers = 1, TimeSpan? timeOut = null,
        int requeueCount = -1, TimeSpan? requeueDelay = null, int unacceptableMessageLimit = 0, 
        MessagePumpType messagePumpType = MessagePumpType.Reactor, IAmAChannelFactory? channelFactory = null, 
        OnMissingChannel makeChannels = OnMissingChannel.Create, TimeSpan? emptyChannelDelay = null, 
        TimeSpan? channelFailureDelay = null, OracleAQMessageType messageType = OracleAQMessageType.Raw,
        OracleAQVisibilityMode visibility = OracleAQVisibilityMode.OnCommit, 
        OracleAQDequeueMode dequeueMode = OracleAQDequeueMode.Locked, string? consumerName = null,
        Encoding? encoding = null) : base(typeof(TMessage), subscriptionName, channelName, routingKey, bufferSize, 
        noOfPerformers, timeOut, requeueCount, requeueDelay, unacceptableMessageLimit, messagePumpType, channelFactory, 
        makeChannels, emptyChannelDelay, channelFailureDelay, messageType, visibility, dequeueMode, consumerName, encoding)
    {
    }
}
