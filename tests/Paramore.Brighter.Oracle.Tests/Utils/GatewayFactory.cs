using Paramore.Brighter.MessagingGateway.Oracle;

namespace Paramore.Brighter.Oracle.Tests.Utils;

public static class GatewayFactory
{
    private const string ConnectionString = "Data Source=localhost:1521/BRIGHTER_DATABASE;User Id=brighter;Password=MyPassword123;";
    public static OracleMessagingGatewayConnection CreateConnection()
    {
        return new OracleMessagingGatewayConnection(ConnectionString);
    }

    // public static async Task<SimpleConsumer> CreateSimpleConsumer(RocketMessagingGatewayConnection connection, Publication publication)
    //     => await CreateSimpleConsumer(connection, publication.Topic!);

    /*public static async Task<SimpleConsumer> CreateSimpleConsumer(RocketMessagingGatewayConnection connection, string topic)
    {
        return await new SimpleConsumer.Builder()
            .SetClientConfig(connection.ClientConfig)
            .SetConsumerGroup(Guid.NewGuid().ToString())
            .SetAwaitDuration(TimeSpan.Zero)
            .SetSubscriptionExpression(new Dictionary<string, FilterExpression> { [topic] = new("*") })
            .Build();
    }

    public static async Task<Producer> CreateProducer(RocketMessagingGatewayConnection connection, Publication publication)
        => await CreateProducer(connection, publication.Topic!);

    public static async Task<Producer> CreateProducer(RocketMessagingGatewayConnection connection, string topic)
    {
        return await new Producer.Builder()
            .SetClientConfig(connection.ClientConfig)
            .SetMaxAttempts(connection.MaxAttempts)
            .SetTopics(topic)
            .Build();
    }*/
}
