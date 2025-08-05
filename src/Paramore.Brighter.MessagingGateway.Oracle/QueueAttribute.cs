namespace Paramore.Brighter.MessagingGateway.Oracle;

public class QueueAttribute
{
    public bool OverrideIfNotExists { get; set; }
    public QueueInformation Queue { get; set; } = new();
    public QueueTable Table { get; set; } = new();
}
