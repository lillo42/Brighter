using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace Paramore.Brighter.Oracle;

/// <summary>
/// Provides a unit of work implementation for Oracle database transactions.
/// </summary>
/// <remarks>
/// This class manages database connections and transactions for Oracle databases,
/// ensuring proper connection lifecycle management and transaction handling.
/// </remarks>
public class OracleUnitOfWork : RelationalDbTransactionProvider
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleUnitOfWork"/> class.
    /// </summary>
    /// <param name="configuration">The database configuration containing the connection string.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the configuration or its connection string is null or whitespace.
    /// </exception>
    public OracleUnitOfWork(IAmARelationalDatabaseConfiguration configuration)
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
        if (Connection == null)
        {
            Connection = new OracleConnection(_connectionString);
        }

        if (Connection.State != ConnectionState.Open)
        {
            Connection.Open();
        }

        return Connection;
    }

    /// <inheritdoc />
    public override async Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (Connection == null)
        {
            Connection = new OracleConnection(_connectionString);
        }

        if (Connection.State != ConnectionState.Open)
        {
            await Connection.OpenAsync(cancellationToken);
        }

        return Connection;
    }
}
