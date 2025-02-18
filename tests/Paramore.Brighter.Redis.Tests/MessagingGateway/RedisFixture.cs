﻿using System;
using System.Threading.Tasks;
using Paramore.Brighter.MessagingGateway.Redis;

namespace Paramore.Brighter.Redis.Tests.MessagingGateway
{
    public class RedisFixture : IAsyncDisposable, IDisposable
    {
        private readonly ChannelName _queueName = new ChannelName("test");
        private readonly RoutingKey _topic = new RoutingKey("test");
        public readonly RedisMessageProducer MessageProducer;
        public readonly RedisMessageConsumer MessageConsumer;

        public RedisFixture()
        {
            RedisMessagingGatewayConfiguration configuration = RedisMessagingGatewayConfiguration();

            MessageProducer = new RedisMessageProducer(configuration, new RedisMessagePublication {Topic = _topic});
            MessageConsumer = new RedisMessageConsumer(configuration, _queueName, _topic);
        }

        public static RedisMessagingGatewayConfiguration RedisMessagingGatewayConfiguration()
        {
            var configuration = new RedisMessagingGatewayConfiguration
            {
                RedisConnectionString = "redis://localhost:6379?ConnectTimeout=1000&SendTimeout=1000",
                MaxPoolSize = 10,
                MessageTimeToLive = TimeSpan.FromMinutes(10),
                DefaultRetryTimeout = 3000
            };
            return configuration;
        }

        public void Dispose()
        {
            MessageConsumer.Purge();
            MessageConsumer.Dispose();
            MessageProducer.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await MessageConsumer.PurgeAsync();
            await MessageConsumer.DisposeAsync();
            await MessageProducer.DisposeAsync();
        }
    }
}
