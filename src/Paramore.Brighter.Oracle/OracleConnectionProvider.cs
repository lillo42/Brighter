using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace Paramore.Brighter.Oracle;

/// <summary>
/// Provides Oracle database connection management for relational database operations.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates and opens a new connection for each request, implementing the "fresh connection per operation" pattern.
/// Unlike connection-reusing implementations, this provider ensures isolation between operations but may have performance implications for high-frequency scenarios.
/// </para>
/// </remarks>
public class OracleConnectionProvider : RelationalDbConnectionProvider
{
    private readonly string _connectionString;
    
    /// <summary>
    /// Initializes a new instance of the Oracle connection provider
    /// </summary>
    /// <param name="configuration">Database configuration source</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the configuration or its ConnectionString property is null or whitespace
    /// </exception>
    /// <example>
    /// <code>
    /// var config = new RelationalDatabaseConfiguration("User Id=test;Password=secret;Data Source=orcl;");
    /// var provider = new OracleConnectionProvider(config);
    /// </code>
    /// </example>
    public OracleConnectionProvider(IAmARelationalDatabaseConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.ConnectionString))
        {
            throw new ArgumentNullException(nameof(configuration.ConnectionString));
        }
        
        _connectionString = configuration.ConnectionString;
    }

    /// <inheritdoc />
    public override DbConnection GetConnection()
    {
        var connection = new OracleConnection(_connectionString);
        if (connection.State != System.Data.ConnectionState.Open)
        {
            connection.Open();
        }
        
        return connection;
    }

    /// <inheritdoc />
    public override async Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new OracleConnection(_connectionString);
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
        
        return connection;
    }
}
