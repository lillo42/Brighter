using System;
using Apache.NMS;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public class ActiveMqPublication : Publication
{
    public TimeProvider? TimeProvider { get; set; }
    public MsgDeliveryMode DeliveryMode { get; set; }
    public MsgPriority Priority { get; set; }
    public TimeSpan TimeToLive { get; set; }
}


public class ActiveMqPublication<T> : ActiveMqPublication 
{
    public ActiveMqPublication()
    {
        RequestType = typeof(T);
    }
}
