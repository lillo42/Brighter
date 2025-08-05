using System;
using Apache.NMS;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public abstract class ActiveMqPublication : Publication
{
    public TimeProvider? TimeProvider { get; set; }
    public MsgDeliveryMode DeliveryMode { get; set; }
    public MsgPriority Priority { get; set; }
    public TimeSpan TimeToLive { get; set; }
}
