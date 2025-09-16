using Apache.NMS.ActiveMQ;
using Paramore.Brighter.MessagingGateway.ActiveMq;

namespace Paramore.Brighter.ActiveMq.Tests.Utils;

public static class GatewayFactory
{
    public static ActiveMqMessagingGatewayConnection CreateConnection()
    {
        var conn = new ActiveMqMessagingGatewayConnection(new ConnectionFactory("activemq:tcp://localhost:61616"));
        
        conn.GetConnection().Start();

        return conn;
    }

    public static ActiveMqMessageProducer CreateProducer(ActiveMqPublication publication)
        => CreateProducer(CreateConnection(), publication);
    public static ActiveMqMessageProducer CreateProducer(ActiveMqMessagingGatewayConnection connection, ActiveMqPublication publication) 
        => new(connection.GetConnection(), publication, connection.TimeProvider);

    public static ActiveMqMessageConsumer CreateConsumer(ActiveMqSubscription subscription)
        => CreateConsumer(CreateConnection(), subscription);
    public static ActiveMqMessageConsumer CreateConsumer(ActiveMqMessagingGatewayConnection connection, ActiveMqSubscription subscription) =>
        new(connection.GetConnection(), subscription);
}
