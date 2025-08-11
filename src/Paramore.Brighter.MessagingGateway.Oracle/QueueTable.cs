using System;
using System.Text;
using Oracle.ManagedDataAccess.Client;

namespace Paramore.Brighter.MessagingGateway.Oracle;

public sealed class QueueTable
{
    public string Name { get; set; } = string.Empty;
    public string PayloadType { get; set; } = string.Empty;
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
    public Action<OracleCommand>? Configuration { get; set; }
}
