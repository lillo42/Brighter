using System;
using System.Threading.Tasks;
using Apache.NMS;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

/// <summary>
/// Implements the <see cref="IAmGatewayConfiguration"/> interface for connecting to an ActiveMQ broker.
/// This class holds the necessary connection details and manages the underlying ActiveMQ <see cref="IConnection"/>.
/// </summary>
public class ActiveMqMessagingGatewayConnection : IAmGatewayConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveMqMessagingGatewayConnection"/> class
    /// with a default <see cref="TimeProvider"/> (System).
    /// </summary>
    public ActiveMqMessagingGatewayConnection()
    {
        
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveMqMessagingGatewayConnection"/> class
    /// with a specified connection factory.
    /// </summary>
    /// <param name="factory">The ActiveMQ connection factory implementation (e.g., from NMS).</param>
    public ActiveMqMessagingGatewayConnection(IConnectionFactory  factory)
    {
        ConnectionFactory = factory;
    }
    
    /// <summary>
    /// Gets or sets the ActiveMQ connection factory used to create new connections.
    /// </summary>
    /// <value>
    /// An instance of <see cref="IConnectionFactory"/>, or <see langword="null"/> if not set.
    /// </value>
    public IConnectionFactory? ConnectionFactory { get; set; }

    /// <summary>
    /// Gets or sets the time provider for time-related operations.
    /// </summary>
    /// <value>
    /// An instance of <see cref="TimeProvider"/>. Defaults to <see cref="TimeProvider.System"/>.
    /// </value>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>
    /// Gets or sets the active ActiveMQ connection instance.
    /// </summary>
    /// <value>
    /// An established <see cref="IConnection"/> instance, or <see langword="null"/> if not connected.
    /// </value>
    public IConnection? Connection { get; set; }

    /// <summary>
    /// Gets the current ActiveMQ connection, creating a new one if it is not already established.
    /// This method is synchronous.
    /// </summary>
    /// <returns>
    /// An established ActiveMQ <see cref="IConnection"/>.
    /// </returns>
    /// <exception cref="ConfigurationException">
    /// Thrown if both the existing <see cref="Connection"/> and the <see cref="ConnectionFactory"/> are <see langword="null"/>.
    /// </exception>
    public IConnection GetConnection()
    {
        if (Connection == null && ConnectionFactory != null)
        {
            throw new ConfigurationException("Connect and ConnectionFactory are null");
        }
        
        Connection ??= ConnectionFactory!.CreateConnection();
        return Connection;
    }
    
    /// <summary>
    /// Asynchronously gets the current ActiveMQ connection, creating a new one if it is not already established.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous connection retrieval operation. The task result is an established ActiveMQ <see cref="IConnection"/>.
    /// </returns>
    /// <exception cref="ConfigurationException">
    /// Thrown if both the existing <see cref="Connection"/> and the <see cref="ConnectionFactory"/> are <see langword="null"/>.
    /// </exception>
    public async Task<IConnection> GetConnectionAsync()
    {
        if (Connection == null && ConnectionFactory != null)
        {
            throw new ConfigurationException("Connect and ConnectionFactory are null");
        }
        
        Connection ??= await ConnectionFactory!.CreateConnectionAsync();
        return Connection;
    }
}
