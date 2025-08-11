using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Types;
using Paramore.Brighter.Extensions;

namespace Paramore.Brighter.MessagingGateway.Oracle.Mappers;

public class UdtMessageMapper<TRequest> : IAmAMessageMapper<TRequest>, IAmAMessageMapperAsync<TRequest>
    where TRequest : class, IRequest, IOracleCustomType 
{
    /// <inheritdoc cref="IAmAMessageMapper{TRequest}.Context"/>
    public IRequestContext? Context { get; set; }
    
    /// <inheritdoc />
    public Task<Message> MapToMessageAsync(TRequest request, Publication publication, CancellationToken cancellationToken = default) 
        => Task.FromResult(MapToMessage(request, publication));

    /// <inheritdoc />
    public Task<TRequest> MapToRequestAsync(Message message, CancellationToken cancellationToken = default) 
        => Task.FromResult(MapToRequest(message));

    /// <inheritdoc />
    public Message MapToMessage(TRequest request, Publication publication)
    {
        if (publication.Topic is null)
        {
            throw new ArgumentException($"No Topic Defined for {publication}");
        }
        
        var messageType = request switch
        {
            ICommand => MessageType.MT_COMMAND,
            IEvent => MessageType.MT_EVENT,
            _ => throw new ArgumentException(@"This message mapper can only map Commands and Events", nameof(request))
        };
        
        var header = new MessageHeader(messageId: request.Id,
            topic: publication.Topic,
            messageType: messageType, 
            contentType: new ContentType("application/json"),
            source: publication.Source,
            type: publication.Type,
            correlationId: request.CorrelationId, 
            replyTo: publication.ReplyTo ?? RoutingKey.Empty, 
            dataSchema: publication.DataSchema, 
            subject: publication.Subject,
            partitionKey: Context.GetPartitionKey(),
            timeStamp: DateTimeOffset.UtcNow);
        
        var defaultHeaders = publication.DefaultHeaders ?? new Dictionary<string, object>();
        header.Bag = defaultHeaders.Merge(Context.GetHeaders());
        header.Bag[HeaderNames.Payload] = request;
        
        return new Message(header, new MessageBody([]));
    }

    /// <inheritdoc />
    public TRequest MapToRequest(Message message)
    {
        if (message.Header.Bag.TryGetValue(HeaderNames.Payload, out var payload)
            && payload is TRequest request)
        {
            return request;
        }

        throw new InvalidOperationException($"Expecting {typeof(TRequest).Name} as payload");
    }
}
