namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public class ActiveMqQueuePublication : ActiveMqPublication;

public class ActiveMqQueuePublication<T> : ActiveMqQueuePublication 
    where T : class, IRequest
{
    public ActiveMqQueuePublication()
    {
        RequestType = typeof(T);
    }
}
