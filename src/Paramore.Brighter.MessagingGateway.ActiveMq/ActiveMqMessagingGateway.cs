using System;
using Apache.NMS;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public class ActiveMqMessagingGateway(IConnectionFactory  factory)  : IAmGatewayConfiguration
{
    public IConnectionFactory ConnectionFactory => factory;
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;
}
