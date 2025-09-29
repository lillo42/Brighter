using Apache.NMS.ActiveMQ;
using Apache.NMS.Policies;
using Paramore.Brighter.MessagingGateway.ActiveMq;

namespace Paramore.Brighter.ActiveMq.Tests.Utils;

public static class GatewayFactory
{
    private static readonly ActiveMqMessagingGatewayConnection s_connection =
        new(new ConnectionFactory("activemq:tcp://localhost:61616"));

    static GatewayFactory()
    {
        s_connection.GetConnection().Start();
    }
    
    public static ActiveMqMessagingGatewayConnection CreateConnection()
    {
        var conn = new ActiveMqMessagingGatewayConnection(new ConnectionFactory("activemq:tcp://localhost:61616")
        {
            RedeliveryPolicy = new RedeliveryPolicy
            {
                MaximumRedeliveries = 30 * 1_000, // 30s
            }
        });
        
        conn.GetConnection().Start();

        return conn;
    }
    
    public static ActiveMqChannelFactory CreateChannel() => new(s_connection);
    public static ActiveMqMessageProducer CreateProducer(ActiveMqPublication publication) => new(s_connection.GetConnection(), publication, s_connection.TimeProvider);
    public static ActiveMqMessageConsumer CreateConsumer(ActiveMqSubscription subscription) => new(s_connection.GetConnection(), subscription);
}
