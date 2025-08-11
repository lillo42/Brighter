using System;
using System.Text;
using Oracle.ManagedDataAccess.Client;

namespace Paramore.Brighter.MessagingGateway.Oracle;

public class QueueInformation
{
    public string Name { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
    public int? QueueType { get; set; }
    public int? MaxRetries { get; set; }
    public int? RetryDelay { get; set; }
    public int? RetentionTime { get; set; }
    public bool? DependencyTracking { get; set; }
    public string? Comment { get; set; }
    public bool? AutoCommit { get; set; }
    public bool? Secure { get; set; }
    public string? Compatible { get; set; }
    public int? PrimaryInstance { get; set; }
    public int? SecondaryInstance { get; set; }
    public QueueAttribute? ExceptionQueue { get; set; }
    public Action<OracleCommand>? Configuration { get; set; }
}
