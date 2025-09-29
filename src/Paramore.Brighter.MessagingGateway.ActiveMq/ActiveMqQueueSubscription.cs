using System;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

/// <summary>
/// Represents a Brighter <see cref="Subscription"/> configuration for consuming messages
/// from an **ActiveMQ Queue**.
/// </summary>
/// <remarks>
/// This configuration is used for point-to-point messaging where a message is intended
/// to be consumed by only one handler instance. It inherits all ActiveMQ-specific 
/// features from <see cref="ActiveMqSubscription"/>.
/// </remarks>
public class ActiveMqQueueSubscription : ActiveMqSubscription
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveMqQueueSubscription"/> class.
    /// </summary>
    /// <param name="subscriptionName">The unique name for this subscription.</param>
    /// <param name="channelName">The name of the channel to be created for this subscription.</param>
    /// <param name="routingKey">The name of the ActiveMQ Queue.</param>
    /// <param name="requestType">The type of <see cref="IRequest"/> the consumer handles.</param>
    /// <param name="getRequestType">A function used to determine the request type from a received message, for polymorphing.</param>
    /// <param name="bufferSize">The number of messages to retrieve from the broker in a single operation (prefetch).</param>
    /// <param name="noOfPerformers">The number of message pumps to run concurrently for this channel.</param>
    /// <param name="timeOut">The amount of time to wait for a message on the channel before timing out.</param>
    /// <param name="requeueCount">The maximum number of times a message should be requeued before being moved to the dead-letter queue (DLQ). Use -1 for infinite.</param>
    /// <param name="requeueDelay">The delay to wait before a failed message is requeued.</param>
    /// <param name="unacceptableMessageLimit">The number of unacceptable messages (parse failures, etc.) allowed before the channel is killed.</param>
    /// <param name="messagePumpType">The type of message pump to use (e.g., default, deferred).</param>
    /// <param name="channelFactory">An optional factory to create the channel, overriding the default.</param>
    /// <param name="makeChannels">Specifies the behavior if the channel doesn't exist.</param>
    /// <param name="emptyChannelDelay">The delay when the channel is empty.</param>
    /// <param name="channelFailureDelay">The delay when an exception occurs on the channel.</param>
    /// <param name="selector">The message selector expression to filter messages received by the consumer.</param>
    /// <param name="noLocal">A flag indicating whether this consumer should suppress messages published by its own connection (Ignored for Queues).</param>
    public ActiveMqQueueSubscription(SubscriptionName subscriptionName,
        ChannelName channelName,
        RoutingKey routingKey,
        Type? requestType = null,
        Func<Message, Type>? getRequestType = null,
        int bufferSize = 1,
        int noOfPerformers = 1,
        TimeSpan? timeOut = null,
        int requeueCount = -1,
        TimeSpan? requeueDelay = null,
        int unacceptableMessageLimit = 0,
        MessagePumpType messagePumpType = MessagePumpType.Unknown,
        IAmAChannelFactory? channelFactory = null,
        OnMissingChannel makeChannels = OnMissingChannel.Create,
        TimeSpan? emptyChannelDelay = null,
        TimeSpan? channelFailureDelay = null,
        string? selector = null,
        bool noLocal = false) : base(subscriptionName, channelName, routingKey, requestType, getRequestType, 
        bufferSize, noOfPerformers, timeOut, requeueCount, requeueDelay, unacceptableMessageLimit, messagePumpType, channelFactory, 
        makeChannels, emptyChannelDelay, channelFailureDelay, selector, noLocal)
    {
    }
}

/// <summary>
/// Represents a strongly-typed Brighter <see cref="Subscription"/> configuration for consuming a specific
/// message type from an ActiveMQ Queue.
/// </summary>
/// <typeparam name="TRequest">The type of the request (<see cref="IRequest"/>) that is expected from the Queue.</typeparam>
public class ActiveMqQueueSubscription<TRequest> : ActiveMqQueueSubscription
    where TRequest : class, IRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveMqQueueSubscription{TRequest}"/> class, 
    /// automatically setting the <see cref="P:Paramore.Brighter.Subscription.RequestType"/> to <typeparamref name="TRequest"/>.
    /// </summary>
    /// <param name="subscriptionName">The unique name for this subscription.</param>
    /// <param name="channelName">The name of the channel to be created for this subscription.</param>
    /// <param name="routingKey">The name of the ActiveMQ Queue.</param>
    /// <param name="getRequestType">A function used to determine the request type from a received message, for polymorphing.</param>
    /// <param name="bufferSize">The number of messages to retrieve from the broker in a single operation (prefetch).</param>
    /// <param name="noOfPerformers">The number of message pumps to run concurrently for this channel.</param>
    /// <param name="timeOut">The amount of time to wait for a message on the channel before timing out.</param>
    /// <param name="requeueCount">The maximum number of times a message should be requeued before being moved to the dead-letter queue (DLQ). Use -1 for infinite.</param>
    /// <param name="requeueDelay">The delay to wait before a failed message is requeued.</param>
    /// <param name="unacceptableMessageLimit">The number of unacceptable messages (parse failures, etc.) allowed before the channel is killed.</param>
    /// <param name="messagePumpType">The type of message pump to use (e.g., default, deferred).</param>
    /// <param name="channelFactory">An optional factory to create the channel, overriding the default.</param>
    /// <param name="makeChannels">Specifies the behavior if the channel doesn't exist.</param>
    /// <param name="emptyChannelDelay">The delay when the channel is empty.</param>
    /// <param name="channelFailureDelay">The delay when an exception occurs on the channel.</param>
    /// <param name="selector">The message selector expression to filter messages received by the consumer.</param>
    /// <param name="noLocal">A flag indicating whether this consumer should suppress messages published by its own connection (Ignored for Queues).</param>
    public ActiveMqQueueSubscription(SubscriptionName subscriptionName,
        ChannelName channelName,
        RoutingKey routingKey,
        Func<Message, Type>? getRequestType = null,
        int bufferSize = 1,
        int noOfPerformers = 1,
        TimeSpan? timeOut = null,
        int requeueCount = -1,
        TimeSpan? requeueDelay = null,
        int unacceptableMessageLimit = 0,
        MessagePumpType messagePumpType = MessagePumpType.Unknown,
        IAmAChannelFactory? channelFactory = null,
        OnMissingChannel makeChannels = OnMissingChannel.Create,
        TimeSpan? emptyChannelDelay = null,
        TimeSpan? channelFailureDelay = null,
        string? selector = null,
        bool noLocal = false) : base(subscriptionName, channelName, routingKey, typeof(TRequest), getRequestType, 
        bufferSize, noOfPerformers, timeOut, requeueCount, requeueDelay, unacceptableMessageLimit, messagePumpType, channelFactory, 
        makeChannels, emptyChannelDelay, channelFailureDelay, selector, noLocal)
    {
    }
}
