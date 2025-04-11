﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Greetings.Ports.Commands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Paramore.Brighter;

namespace GreetingsSender
{
    public class TimedMessageGenerator(IAmACommandProcessor processor, ILogger<TimedMessageGenerator> logger)
        : IHostedService, IDisposable
    {
        private Timer _timer;
        private long _iteration = 0;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Kafka Message Generator is starting");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Kafka Message Generator is stopping");

            _timer?.Change(Timeout.Infinite, 60_000);

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            _iteration++;

            if (_iteration % 2 == 0)
            {
                var greetingEvent =
                    new GreetingEvent { Id = Guid.NewGuid().ToString(), Greeting = $"Hello # {_iteration}" };

                processor.PostAsync(greetingEvent).GetAwaiter().GetResult();

                logger.LogInformation("Sending message with id {Id} and greeting {Request}", greetingEvent.Id,
                    greetingEvent.Greeting);
            }
            else
            {
                var greetingEvent =
                    new AnotherGreetingEvent { Id = Guid.NewGuid().ToString(), Greeting = $"Hello # {_iteration}" };

                processor.PostAsync(greetingEvent).GetAwaiter().GetResult();

                logger.LogInformation("Sending message with id {Id} and greeting {Request}", greetingEvent.Id,
                    greetingEvent.Greeting);
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
