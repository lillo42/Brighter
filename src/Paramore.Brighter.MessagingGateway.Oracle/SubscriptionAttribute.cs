namespace Paramore.Brighter.MessagingGateway.Oracle;

public class SubscriptionAttribute
{
    public string QueueName { get; set; } = string.Empty;
    public string ConsumerName { get; set; } = string.Empty;
    public string? Rule { get; set; }
    public string? Transformation { get; set; }
    
    public bool? QueueToQueue { get; set; }
    public int? DeliveryMode { get; set; }
}
