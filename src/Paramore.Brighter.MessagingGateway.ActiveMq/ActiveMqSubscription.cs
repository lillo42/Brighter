using System;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public abstract class ActiveMqSubscription(
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
    bool noLocal = false)
    : Subscription(subscriptionName,
        channelName, routingKey, requestType, getRequestType, bufferSize, noOfPerformers, timeOut, requeueCount,
        requeueDelay, unacceptableMessageLimit, messagePumpType, channelFactory, makeChannels, emptyChannelDelay,
        channelFailureDelay)
{
    public string? Selector { get; } = selector;
    public bool NoLocal { get; } = noLocal;
}
