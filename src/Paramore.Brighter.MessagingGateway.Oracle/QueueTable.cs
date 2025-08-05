using System;
using System.Text;
using Oracle.ManagedDataAccess.Client;

namespace Paramore.Brighter.MessagingGateway.Oracle;

public sealed class QueueTable
{
    public string Name { get; set; } = string.Empty;
    public string PayloadType { get; set; } = string.Empty;
    public string? PayloadTypeCreateOrUpdateQuery { get; set; }
    public string? StorageClause { get; set; }
    public string? SortList { get; set; }
    public bool? MultipleConsumers { get; set; }
    public int? MessageGrouping  { get; set; }
    public string? Comment { get; set; }
    public bool? AutoCommit { get; set; }
    public int? PrimaryInstance { get; set; }
    public int? SecondaryInstance { get; set; }
    public string? Compatible { get; set; }
    public bool? Secure { get; set; }
    public int? RetentionTime { get; set; }
    public bool? DependencyTracking { get; set; }
    public int? MaxRetries { get; set; }
    public int? RetryDelay { get; set; }
    public string? Users { get; set; }
    public string? UserComment { get; set; }
    
    public Action<StringBuilder>? Configuration { get; set; }
}
