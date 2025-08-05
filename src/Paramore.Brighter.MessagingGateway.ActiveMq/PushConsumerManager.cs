using System.Threading.Channels;
using Apache.NMS;

namespace Paramore.Brighter.MessagingGateway.ActiveMq;

internal class PushConsumerManager
{
    private readonly ISession _session;
    private readonly INMSConsumer _consumer;
    private readonly Channel<IMessage> _channel = System.Threading.Channels.Channel.CreateUnbounded<IMessage>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false,
        AllowSynchronousContinuations = true
    });

    public PushConsumerManager(ISession session, INMSConsumer consumer)
    {
        _session = session;
        _consumer = consumer;

        _consumer.Listener += OnMessageReceived;
    }


    private void OnMessageReceived(IMessage message) => _channel.Writer.TryWrite(message);
    public ChannelReader<IMessage> Reader => _channel.Reader;
}
