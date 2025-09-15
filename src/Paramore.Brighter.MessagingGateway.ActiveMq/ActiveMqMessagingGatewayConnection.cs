using System;
using System.Threading.Tasks;
using Apache.NMS;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

public class ActiveMqMessagingGatewayConnection(IConnectionFactory  factory)  : IAmGatewayConfiguration
{
    
    public IConnectionFactory ConnectionFactory => factory;
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    private IConnection? _connection;

    internal IConnection GetConnection()
    {
        _connection ??= ConnectionFactory.CreateConnection();
        return _connection;
    }
    
    internal async Task<IConnection> GetConnectionAsync()
    {
        _connection ??= await ConnectionFactory.CreateConnectionAsync();
        return _connection;
    }
}
