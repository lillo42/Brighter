namespace Paramore.Brighter.MessagingGateway.Oracle;

public class OracleTransactionalEventQueuePublication : OracleAdvanceQueuePublication;

public class OracleTransactionalEventQueuePublication<TRequest> : OracleTransactionalEventQueuePublication
    where TRequest : class, IRequest 
{
    public OracleTransactionalEventQueuePublication()
    {
        RequestType = typeof(TRequest);
    }
}
