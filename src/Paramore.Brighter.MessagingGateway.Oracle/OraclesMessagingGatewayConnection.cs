using Oracle.ManagedDataAccess.Client;

namespace Paramore.Brighter.MessagingGateway.Oracle;

/// <summary>
/// Represents the configuration for Oracle Advanced Queuing (AQ) messaging gateways
/// </summary>
/// <remarks>
/// <para>
/// This class encapsulates the essential configuration settings required to establish
/// connections and define behavior for Oracle AQ messaging operations. It combines
/// standard relational database configuration with Oracle-specific messaging extensions.
/// </para>
/// <para>
/// Typical usage includes:
/// <list type="number">
///   <item>Configure database connection parameters via <see cref="Configuration"/></item>
///   <item>Optionally set sender identification using <see cref="Sender"/></item>
///   <item>Pass the configuration to messaging producers/consumers</item>
/// </list>
/// </para>
/// </remarks>
/// <param name="configuration">The relational database configuration</param>
public class OraclesMessagingGatewayConnection(IAmARelationalDatabaseConfiguration configuration) : IAmGatewayConfiguration
{
    
    /// <summary>
    /// Gets the relational database configuration
    /// </summary>
    /// <value>
    /// Instance of <see cref="IAmARelationalDatabaseConfiguration"/> containing:
    /// <list type="bullet">
    ///   <item>Connection string</item>
    ///   <item>Queue/store names</item>
    ///   <item>Authentication credentials</item>
    ///   <item>Database schema information</item>
    /// </list>
    /// </value>
    public IAmARelationalDatabaseConfiguration Configuration { get; } = configuration;
    
    /// <summary>
    /// Gets or sets the sender identification metadata
    /// </summary>
    /// <remarks>
    /// <para>
    /// Optional agent information identifying the message originator. When set,
    /// this information appears in Oracle queue tables as:
    /// </para>
    /// <list type="bullet">
    ///   <item>SENDER_NAME</item>
    ///   <item>SENDER_ADDRESS</item>
    ///   <item>SENDER_PROTOCOL</item>
    /// </list>
    /// <para>
    /// Example usage:
    /// <code>
    /// config.Sender = new OracleAQAgent("ServiceName", "Instance01");
    /// </code>
    /// </para>
    /// </remarks>
    public OracleAQAgent? Sender { get; set; }
}
