using Oracle.ManagedDataAccess.Client;

namespace Paramore.Brighter.MessagingGateway.Oracle;

public class OracleAdvanceQueuePublication : Publication
{
    public string Queue { get; set; } = string.Empty;
    
    public OracleAQMessageType MessageType { get; set; } = OracleAQMessageType.Raw;
  
    public OracleAQMessageDeliveryMode DeliveryMode { get; set; } = OracleAQMessageDeliveryMode.Persistent;
    
    public OracleAQVisibilityMode VisibilityMode { get; set; } = OracleAQVisibilityMode.OnCommit;
    
    public string? UdtTypeName { get; set; }
    
    public int Expiration { get; set; }
    
    public int Priority { get; set; } = 5;
    
    public OracleAQAgent? Sender { get; set; }
    
    public QueueAttribute? Attribute { get; set; }
}

public class OracleAdvanceQueuePublication<TRequest> : OracleAdvanceQueuePublication
    where TRequest : class, IRequest 
{
    public OracleAdvanceQueuePublication()
    {
        RequestType = typeof(TRequest);
    }
}
