using System;
using System.Text;
using Oracle.ManagedDataAccess.Client;

namespace Paramore.Brighter.MessagingGateway.Oracle;

public class OracleTransactionalEventQueueSubscription(
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
    MessagePumpType messagePumpType = MessagePumpType.Reactor,
    IAmAChannelFactory? channelFactory = null,
    OnMissingChannel makeChannels = OnMissingChannel.Create,
    TimeSpan? emptyChannelDelay = null,
    TimeSpan? channelFailureDelay = null,
    string? consumerName = null,
    string? correlation = null,
    OracleAQMessageDeliveryMode deliveryMode = OracleAQMessageDeliveryMode.Persistent,
    int messageIdLength = 16,
    OracleAQDequeueMode dequeueMode = OracleAQDequeueMode.Remove,
    OracleAQNavigationMode navigationMode = OracleAQNavigationMode.NextMessage,
    bool providerSpecificType = false,
    OracleAQVisibilityMode visibility = OracleAQVisibilityMode.OnCommit,
    string? udtTypeName = null,
    OracleAQMessageType messageType = OracleAQMessageType.Raw,
    Encoding? encoding = null,
    QueueAttribute? attribute = null)
    : OracleAdvanceQueueSubscription(subscriptionName, channelName, routingKey, requestType, getRequestType, bufferSize,
        noOfPerformers, timeOut, requeueCount, requeueDelay, unacceptableMessageLimit, messagePumpType, channelFactory,
        makeChannels, emptyChannelDelay, channelFailureDelay, consumerName, correlation, deliveryMode, messageIdLength,
        dequeueMode, navigationMode, providerSpecificType, visibility, udtTypeName, messageType, encoding, attribute);

public class OracleTransactionalEventQueueSubscription<TRequest>(
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
    MessagePumpType messagePumpType = MessagePumpType.Reactor,
    IAmAChannelFactory? channelFactory = null,
    OnMissingChannel makeChannels = OnMissingChannel.Create,
    TimeSpan? emptyChannelDelay = null,
    TimeSpan? channelFailureDelay = null,
    string? consumerName = null,
    string? correlation = null,
    OracleAQMessageDeliveryMode deliveryMode = OracleAQMessageDeliveryMode.Persistent,
    int messageIdLength = 16,
    OracleAQDequeueMode dequeueMode = OracleAQDequeueMode.Remove,
    OracleAQNavigationMode navigationMode = OracleAQNavigationMode.NextMessage,
    bool providerSpecificType = false,
    OracleAQVisibilityMode visibility = OracleAQVisibilityMode.OnCommit,
    string? udtTypeName = null,
    OracleAQMessageType messageType = OracleAQMessageType.Raw,
    Encoding? encoding = null,
    QueueAttribute? attribute = null)
    : OracleTransactionalEventQueueSubscription(subscriptionName, channelName, routingKey, typeof(TRequest),
        getRequestType, bufferSize, noOfPerformers, timeOut, requeueCount, requeueDelay, unacceptableMessageLimit, 
        messagePumpType, channelFactory, makeChannels, emptyChannelDelay, channelFailureDelay, consumerName, correlation, 
        deliveryMode, messageIdLength, dequeueMode, navigationMode, providerSpecificType, visibility, udtTypeName,
        messageType, encoding, attribute)
    where TRequest : class, IRequest;
