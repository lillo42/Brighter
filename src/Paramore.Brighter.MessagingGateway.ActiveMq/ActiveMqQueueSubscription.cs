using System;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public class ActiveMqQueueSubscription(
    SubscriptionName subscriptionName,
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
    bool noLocal = false) : ActiveMqSubscription(subscriptionName, channelName, routingKey, requestType, getRequestType, 
    bufferSize, noOfPerformers, timeOut, requeueCount, requeueDelay, unacceptableMessageLimit, messagePumpType, channelFactory, 
    makeChannels, emptyChannelDelay, channelFailureDelay, selector, noLocal);

public class ActiveMqQueueSubscription<TRequest>(
    SubscriptionName subscriptionName,
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
    bool noLocal = false) : ActiveMqQueueSubscription(
    subscriptionName, channelName, routingKey, typeof(TRequest), getRequestType, 
    bufferSize, noOfPerformers, timeOut, requeueCount, requeueDelay, unacceptableMessageLimit, messagePumpType, channelFactory, 
    makeChannels, emptyChannelDelay, channelFailureDelay, selector, noLocal)
    where TRequest : class, IRequest;
