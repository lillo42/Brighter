namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public class ActiveMqTopicPublication : ActiveMqPublication;

public class ActiveMqTopicPublication<T> : ActiveMqTopicPublication 
    where T : class, IRequest
{
    public ActiveMqTopicPublication()
    {
        RequestType = typeof(T);
    }
}
