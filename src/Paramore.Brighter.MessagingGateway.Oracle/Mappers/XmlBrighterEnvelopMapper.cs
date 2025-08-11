using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Paramore.Brighter.Extensions;

namespace Paramore.Brighter.MessagingGateway.Oracle.Mappers;

public class XmlBrighterEnvelopMapper<TRequest> : IAmAMessageMapper<TRequest>, IAmAMessageMapperAsync<TRequest>
    where TRequest : class, IRequest
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
        
        var obj = new BrighterEnvelop
        {
            Id = request.Id, 
            Topic = publication.Topic!, 
            Source = publication.Source.ToString(), 
            ContentType = "application/xml",
            MessageType = messageType.ToString(),
            TimeStamp = DateTimeOffset.UtcNow.ToRfc3339(),
        };
        
        if (!Id.IsNullOrEmpty(request.CorrelationId))
        {
            obj.CorrelationId = request.CorrelationId;
        }
        
        if (publication.Type != CloudEventsType.Empty)
        {
            obj.Type = publication.Type;
        }
        
        var partitionKey = Context.GetPartitionKey();
        if (partitionKey != PartitionKey.Empty)
        {
            obj.PartitionKey = partitionKey;
        }

        if (publication.ReplyTo != null)
        {
            obj.ReplyTo = publication.ReplyTo;
        }

        if (!string.IsNullOrEmpty(publication.Subject))
        {
            obj.Subject = publication.Subject;
        }

        if (publication.DataSchema != null)
        {
            obj.DataSchema = publication.DataSchema.ToString();
        }

        var header = new MessageHeader(messageId: request.Id,
            topic: publication.Topic,
            messageType: messageType,
            contentType: new ContentType("application/xml"),
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

        obj.Headers = header.Bag
            .Where(x => 
                x.Key != HeaderNames.Expiration 
                && x.Key != HeaderNames.Payload
                && x.Key != HeaderNames.Priority)
            .Select(x => new KeyValueElement
            {
                Key = x.Key,
                Value = x.Value.ToString()
            }).ToArray();
        
        var serializer = new XmlSerializer(typeof(BrighterEnvelop));
        using var writer = new StringWriter();
        serializer.Serialize(writer, obj);

        return new Message(new MessageHeader(messageId: request.Id,
            topic: publication.Topic,
            messageType: messageType, 
            contentType: new ContentType("application/xml"),
            source: publication.Source,
            type: publication.Type,
            correlationId: request.CorrelationId, 
            replyTo: publication.ReplyTo ?? RoutingKey.Empty, 
            dataSchema: publication.DataSchema, 
            subject: publication.Subject,
            partitionKey: Context.GetPartitionKey(),
            timeStamp: DateTimeOffset.UtcNow), 
            new MessageBody(writer.ToString()));
    }

    /// <inheritdoc />
    public TRequest MapToRequest(Message message)
    {
        var serializer = new XmlSerializer(typeof(BrighterEnvelop));
        using var reader = new StringReader(message.Body.Value);
        var envelop = (BrighterEnvelop)serializer.Deserialize(reader)!;
        return envelop.Data!;
    }

    [XmlRoot(ElementName = "Brighter")]
    internal sealed class BrighterEnvelop
    {
        [XmlElement("Id")]
        public string Id { get; set; } = string.Empty;
        
        [XmlElement("ContentType")]
        public string ContentType { get; set; } = string.Empty;
        
        [XmlElement("CorrelationId")]
        public string CorrelationId { get; set; } = string.Empty;
        
        [XmlElement("Topic")]
        public string Topic { get; set; } = string.Empty;
        
        [XmlElement("DataRef", IsNullable = true)]
        public string? DataRef { get; set; }
        
        [XmlElement("PartitionKey", IsNullable = true)]
        public string? PartitionKey { get; set; }
        
        [XmlElement("ReplyTo", IsNullable = true)]
        public string? ReplyTo { get; set; }

        [XmlElement("MessageType", IsNullable = true)]
        public string MessageType { get; set; } = string.Empty;

        [XmlElement("SpecVersion")]
        public string SpecVersion { get; set; } = "1.0";

        [XmlElement("Source")]
        public string Source { get; set; } = string.Empty;
        
        [XmlElement("Type", IsNullable = true)]
        public string? Type { get; set; }
        
        [XmlElement("DataSchema", IsNullable = true)]
        public string? DataSchema { get; set; }
        
        [XmlElement("Subject", IsNullable = true)]
        public string? Subject { get; set; }
        
        [XmlElement("TimeStamp")]
        public string TimeStamp { get; set; } = string.Empty;

        [XmlArray(ElementName = "Headers")]
        [XmlArrayItem(ElementName = "Header")]
        public KeyValueElement[]? Headers { get; set; }
        
        [XmlElement("Data", IsNullable = true)]
        public TRequest? Data { get; set; }
    }
    
    [Serializable]
    internal sealed class KeyValueElement
    {
        [XmlAttribute("Key")]
        public string Key { get; set; } = string.Empty;
        
        [XmlElement("Value", IsNullable = true)]
        public string? Value { get; set; } 
    }
}
