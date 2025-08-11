using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Paramore.Brighter.Extensions;
using Paramore.Brighter.JsonConverters;
using Paramore.Brighter.Logging;
using Paramore.Brighter.MessagingGateway.Oracle.Transforms.Attributes;

namespace Paramore.Brighter.MessagingGateway.Oracle.Transforms.Transformers;

public partial class BrighterEnvelopTransformer : IAmAMessageTransform, IAmAMessageTransformAsync
{
    private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<BrighterEnvelop>();

    private BrighterEnvelopType _envelopType;

    /// <inheritdoc cref="IAmAMessageTransform.Context"/>
    public IRequestContext? Context { get; set; }

    /// <inheritdoc cref="IAmAMessageTransform.InitializeWrapFromAttributeParams"/>
    public void InitializeWrapFromAttributeParams(params object?[] initializerList)
    {
        if (initializerList.Length != 1)
        {
            return;
        }

        if (initializerList[0] is BrighterEnvelopType envelopType)
        {
            _envelopType = envelopType;
        }
    }

    /// <inheritdoc cref="IAmAMessageTransform.InitializeUnwrapFromAttributeParams" />
    public void InitializeUnwrapFromAttributeParams(params object?[] initializerList) { }

    /// <inheritdoc />
    public Task<Message> WrapAsync(Message message, Publication publication, CancellationToken cancellationToken) 
        => Task.FromResult(Wrap(message, publication));

    public Task<Message> UnwrapAsync(Message message, CancellationToken cancellationToken) 
        => Task.FromResult(Unwrap(message));

    /// <inheritdoc />
    public Message Wrap(Message message, Publication publication)
    {
        if (_envelopType == BrighterEnvelopType.Json)
        {
            return JsonWrap(message);
        }

        if (_envelopType == BrighterEnvelopType.Xml)
        {
            return XmlWrap(message);
        }

        return message;
    }

    private Message JsonWrap(Message message)
    {
        try
        {
            JsonElement? data = null;
            string? dataBase64 = null;
            var contentType = message.Header.ContentType.ToString();
            if (message.Body.Value.Length > 0)
            {
                if (contentType.Contains("application/json") || contentType.Contains("text/json"))
                {
                    data = JsonSerializer.Deserialize<JsonElement>(message.Body.Value, JsonSerialisationOptions.Options);
                }
                else if (contentType.Contains("application/octet-stream"))
                {
                    // Base64 encode binary data and use data_base64
                    dataBase64 = Convert.ToBase64String(message.Body.Bytes);
                }
                else
                {
                    // Properly encode the value as a JSON string
                    var encoded = JsonEncodedText.Encode(message.Body.Value);
                    data = JsonDocument.Parse($"\"{encoded.ToString()}\"").RootElement;
                }
            }
            
            var headers =  new Dictionary<string, object>();

            var cloudEvent = new BrighterEnvelop
            {
                Id = message.Id,
                Topic = message.Header.Topic,
                MessageType = message.Header.MessageType,
                PartitionKey = message.Header.PartitionKey,
                CorrelationId = message.Header.CorrelationId,
                SpecVersion = message.Header.SpecVersion,
                Source = message.Header.Source,
                Type = message.Header.Type,
                DataContentType = contentType,
                DataSchema = message.Header.DataSchema,
                DataRef = message.Header.DataRef,
                Subject = message.Header.Subject,
                TimeStamp = message.Header.TimeStamp,
                ReplyTo = message.Header.ReplyTo,
                Headers = headers
                    .Merge(Context.GetHeaders()),
                Data = data,
                DataBase64 = dataBase64 
            };

            message.Body = new MessageBody(JsonSerializer.SerializeToUtf8Bytes(cloudEvent, JsonSerialisationOptions.Options));
            message.Header.ContentType = new ContentType("application/brighter+json");

            return message;
        }
        catch (JsonException e)
        {
            Log.ErrorDuringDeserializerAJsonOnWrap(s_logger, e);
            return message;
        }
    }

    private Message XmlWrap(Message message)
    {
        try
        {
            XElement? data = null;
            XElement? dataBase64 = null;
            var contentType = message.Header.ContentType.ToString();
            if (message.Body.Value.Length > 0)
            {
                if (contentType.Contains("application/xml") || contentType.Contains("text/xml"))
                {
                    data = new XElement("Data", XDocument.Parse(message.Body.Value));
                }
                else if (contentType.Contains("application/octet-stream"))
                {
                    // Base64 encode binary data and use data_base64
                    dataBase64 = new XElement("DataBase64", Convert.ToBase64String(message.Body.Bytes));
                }
                else
                {
                    // Properly encode the value as a JSON string
                    data = new XElement("Data", message.Body.Value);
                }
            }

            var doc = new XDocument(
                new XDeclaration("10.0", "utf-8", "yes"),
                new XElement("Brighter",
                new XElement("Id", message.Header.MessageId.Value),
                new XElement(nameof(MessageHeader.Topic), message.Header.Topic.Value),
                new XElement(nameof(MessageHeader.MessageType), message.Header.MessageType.ToString()),
                new XElement(nameof(MessageHeader.SpecVersion), message.Header.SpecVersion),
                new XElement(nameof(MessageHeader.Source), message.Header.Source.ToString()),
                new XElement(nameof(MessageHeader.ContentType), message.Header.ContentType.ToString()),
                new XElement(nameof(MessageHeader.TimeStamp), message.Header.TimeStamp.ToRfc3339()))
            );

            var root = doc.Root!;
            if (message.Header.PartitionKey != PartitionKey.Empty)
            {
                root.Add(new XElement(nameof(MessageHeader.PartitionKey), message.Header.PartitionKey.Value));
            }
            
            if (!Id.IsNullOrEmpty(message.Header.CorrelationId))
            {
                root.Add(new XElement(nameof(MessageHeader.CorrelationId), message.Header.CorrelationId.Value));
            }

            if (!RoutingKey.IsNullOrEmpty(message.Header.ReplyTo))
            {
                root.Add(new XElement(nameof(MessageHeader.ReplyTo), message.Header.ReplyTo.Value));
            }

            if (!string.IsNullOrEmpty(message.Header.Subject))
            {
                root.Add(new XElement(nameof(MessageHeader.Subject), message.Header.Subject));
            }
            
            if (message.Header.DataSchema != null)
            {
                root.Add(new XElement(nameof(MessageHeader.DataSchema), message.Header.DataSchema.ToString()));
            }

            if (!string.IsNullOrEmpty(message.Header.DataRef))
            {
                root.Add(new XElement(nameof(MessageHeader.DataRef), message.Header.DataRef));
            }

            var header = new XElement("Headers");
            foreach (var keyPair in message.Header.Bag.Merge(Context.GetHeaders()))
            {
                header.Add(new XElement("Header",
                    new XAttribute("Key", keyPair.Key),
                    keyPair.Value.ToString()));
            }
            
            root.Add(header);

            if (data != null)
            {
                root.Add(data);
            }

            if (dataBase64 != null)
            {
                root.Add(dataBase64);
            }
            
            message.Body = new MessageBody(doc.ToString(), new ContentType("application/brighter+xml"));
            return message;
        }
        catch (Exception e)
        {
            Log.ErrorDuringDeserializerAXMlOnWrap(s_logger, e);
            return message;
        }
    }

    /// <inheritdoc />
    public Message Unwrap(Message message)
    {
        if (_envelopType == BrighterEnvelopType.Json)
        {
            return JsonUnwrap(message);
        }

        if (_envelopType == BrighterEnvelopType.Xml)
        {
            return XmlUnwrap(message);
        }
        
        return message;
    }
    
    private static Message JsonUnwrap(Message message)
    {
        try
        {
            var envelop = JsonSerializer.Deserialize<BrighterEnvelop>(message.Body.Bytes, JsonSerialisationOptions.Options);
            if (envelop == null)
            {
                return message;
            }

            var bag = new Dictionary<string, object>(envelop.Headers ?? new Dictionary<string, object>());
            foreach (var pair in message.Header.Bag)
            {
                bag[pair.Key] = pair.Value;
            }

            var header = new MessageHeader
            {
                MessageId = envelop.Id,
                SpecVersion = envelop.SpecVersion,
                Source = envelop.Source,
                Type = new CloudEventsType(envelop.Type),
                ContentType = new ContentType(envelop.DataContentType!),
                DataSchema = envelop.DataSchema,
                Subject = envelop.Subject,
                TimeStamp = envelop.TimeStamp ?? DateTimeOffset.UtcNow,
                CorrelationId = envelop.CorrelationId,
                DataRef = envelop.DataRef ?? message.Header.DataRef,
                Delayed = message.Header.Delayed,
                HandledCount = message.Header.HandledCount,
                MessageType = envelop.MessageType,
                PartitionKey = envelop.PartitionKey,
                ReplyTo = envelop.ReplyTo,
                Topic = envelop.Topic,
                TraceParent = message.Header.TraceParent,
                TraceState = message.Header.TraceState,
                Bag = bag
            };
           
            MessageBody body;
            if (!string.IsNullOrEmpty(envelop.DataBase64))
            {
                // Binary data: decode from base64
                var bytes = Convert.FromBase64String(envelop.DataBase64!);
                body = new MessageBody(bytes);
            }
            else if (envelop.Data.HasValue)
            {
                // JSON or string data
                body = envelop.Data.Value.ValueKind == JsonValueKind.String 
                    ? new MessageBody(envelop.Data.Value.GetString() ?? string.Empty) 
                    : new MessageBody(envelop.Data.Value.GetRawText());
            }
            else
            {
                body = new MessageBody(string.Empty);
            }
            
            return new Message(header, body);
        }
        catch(JsonException ex)
        {
            Log.ErrorDuringDeserializerOnUnwrap(s_logger, ex);
            return message;
        }
    }

    private Message XmlUnwrap(Message message)
    {
        try
        {
            var doc = XDocument.Parse(message.Body.Value);

            var root = doc.Root;
            if (root == null || root.Name != "Brighter")
            {
                return message;
            }

            var body = new MessageBody(string.Empty);
            var element = root.Element("DataBase64");
            if (element != null)
            {
                body = new MessageBody(Convert.FromBase64String(element.Value)); 
            }
            
            element = root.Element("Data");
            if (element != null)
            {
                body = new MessageBody(element.ToString());
            }

            var bag = new Dictionary<string, object>();
            element = root.Element("Headers");
            if (element != null)
            {
                foreach (var pair in element.Elements())
                {
                    var key = pair.Attribute("Key");
                    if (key != null)
                    {
                        bag[key.Value] = pair.Value;
                    }
                }
            }
            
            return new Message(
                new MessageHeader(
                    messageId: GetId(root),
                    topic: GetTopic(root, message),
                    messageType: GetMessageType(root),
                    partitionKey: GetPartitionKey(root),
                    correlationId: GetCorrelationId(root),
                    source: GetSource(root),
                    type: GetCeType(root),
                    contentType: GetContentType(root),
                    dataSchema: GetDataSchema(root),
                    subject: GetSubject(root), 
                    timeStamp: GetTimeStamp(root),
                    replyTo: GetReplyTo(root),
                    handledCount: message.Header.HandledCount,
                    delayed: message.Header.Delayed
                )
                {
                    SpecVersion = GetSpecVersion(root),
                    DataRef = GetDataRef(root),
                    Bag = bag
                },
               body 
            );
        }
        catch (Exception ex)
        {
            Log.ErrorDuringXMLDeserializerOnUnwrap(s_logger, ex);
            return message;
        }

        static Id GetId(XElement root)
        {
            var element = root.Element("Id");
            return element != null ? Id.Create(element.Value) : Id.Random();
        }

        static RoutingKey GetTopic(XElement root, Message message)
        {
            var element =  root.Element(nameof(MessageHeader.Topic));
            return element != null ? new RoutingKey(element.Value) : message.Header.Topic;
        }

        static MessageType GetMessageType(XElement root)
        {
            var element = root.Element(nameof(MessageHeader.MessageType));
            if (element != null && Enum.TryParse<MessageType>(root.Value, true, out var messageType))
            {
                return messageType;
            }

            return MessageType.MT_EVENT;
        }
        
        static PartitionKey GetPartitionKey(XElement root)
        {
            var element = root.Element(nameof(MessageHeader.PartitionKey));
            return element != null ? new PartitionKey(element.Value) : PartitionKey.Empty;
        }
        
        static Id GetCorrelationId(XElement root)
        {
            var element = root.Element(nameof(MessageHeader.CorrelationId));
            return element != null ? Id.Create(element.Value) : Id.Empty;
        }
        
        static string GetSpecVersion(XElement root)
        {
            var element = root.Element(nameof(MessageHeader.SpecVersion));
            return element != null ? element.Value : MessageHeader.DefaultSpecVersion;
        }
        
        static Uri GetSource(XElement root)
        {
            var element = root.Element(nameof(MessageHeader.Source));
            if (element != null && Uri.TryCreate(element.Value, UriKind.RelativeOrAbsolute, out var uri))
            {
                return uri;
            }

            return new Uri(MessageHeader.DefaultSource);
        }
        
        static CloudEventsType GetCeType(XElement root)
        {
            var element = root.Element(nameof(MessageHeader.Type));
            return element != null ? new CloudEventsType(element.Value) : CloudEventsType.Empty;
        }
        
        static ContentType GetContentType(XElement root)
        {
            var element = root.Element(nameof(MessageHeader.ContentType));
            return element != null ? new ContentType(element.Value) : new ContentType("application/xml");
        }
        
        static Uri? GetDataSchema(XElement root)
        {
            var element = root.Element(nameof(MessageHeader.DataSchema));
            if (element != null && Uri.TryCreate(element.Value, UriKind.RelativeOrAbsolute, out var uri))
            {
                return uri;
            }

            return null;
        }
        
        static string? GetDataRef(XElement root)
        {
            var element = root.Element(nameof(MessageHeader.DataRef));
            return element?.Value;
        }
        
        static string? GetSubject(XElement root)
        {
            var element = root.Element(nameof(MessageHeader.Subject));
            return element?.Value;
        }
        
        static DateTimeOffset GetTimeStamp(XElement root)
        {
            var element = root.Element(nameof(MessageHeader.TimeStamp));
            if (element != null && DateTimeOffset.TryParse(element.Value, out var dateTimeOffset))
            {
                return dateTimeOffset;
            }
            
            return DateTimeOffset.UtcNow;
        }
        
        static RoutingKey? GetReplyTo(XElement root)
        {
            var element = root.Element(nameof(MessageHeader.ReplyTo));
            if (element != null)
            {
                return new RoutingKey(element.Value);
            }
            
            return null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        //no op as we have no unmanaged resources
    }

    internal sealed class BrighterEnvelop
    {
        /// <summary>
        /// Gets or sets the unique identifier for the event.
        /// Complies with the 'id' attribute of the CloudEvents specification.
        /// Defaults to an empty string.
        /// </summary>
        [JsonRequired]
        [JsonPropertyName("id")]
        public Id Id { get; set; } = Id.Empty;
        
        [JsonRequired]
        [JsonPropertyName("topic")]
        public RoutingKey Topic { get; set; } = RoutingKey.Empty;
        
        [JsonRequired]
        [JsonPropertyName("messageType")]
        public MessageType MessageType { get; set; } = MessageType.MT_EVENT;
        
        [JsonPropertyName("partitionKey")]
        public PartitionKey PartitionKey { get; set; } = PartitionKey.Empty;
        
        [JsonPropertyName("correlationId")]
        public Id CorrelationId { get; set; } = Id.Empty;
        
        [JsonPropertyName("dataRef")]
        public string? DataRef { get; set; }
        
        [JsonPropertyName("replyTo")]
        public RoutingKey? ReplyTo { get; set; }

        
        /// <summary>
        /// Gets or sets the specification version of the CloudEvents specification which the event uses.
        /// Complies with the 'specversion' attribute.
        /// Defaults to "1.0".
        /// </summary>
        [JsonRequired]
        [JsonPropertyName("specversion")]
        public string SpecVersion { get; set; } = "1.0";

        /// <summary>
        /// Gets or sets the source of the event.
        /// Complies with the 'source' attribute, identifying the context in which an event happened.
        /// Defaults to the <see cref="MessageHeader.DefaultSource"/>.
        /// </summary>
        [JsonRequired]
        [JsonPropertyName("source")]
        public Uri Source { get; set; } = new(MessageHeader.DefaultSource);

        /// <summary>
        /// Gets or sets the type of the event.
        /// Complies with the 'type' attribute, describing the kind of event related to the originating occurrence.
        /// Defaults to an empty string.
        /// </summary>
        [JsonRequired]
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content type of the 'data' payload.
        /// Complies with the optional 'datacontenttype' attribute.
        /// Examples include "application/json", "application/xml", etc.
        /// </summary>
        [JsonPropertyName("datacontenttype")]
        public string? DataContentType { get; set; }

        /// <summary>
        /// Gets or sets the schema URI for the 'data' payload.
        /// Complies with the optional 'dataschema' attribute, providing a link to the schema definition.
        /// </summary>
        [JsonPropertyName("dataschema")]
        public Uri? DataSchema { get; set; }

        /// <summary>
        /// Gets or sets the subject of the event in the context of the event producer.
        /// Complies with the optional 'subject' attribute.
        /// </summary>
        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the occurrence took place.
        /// Complies with the optional 'time' attribute.
        /// </summary>
        [JsonPropertyName("timeStamp")]
        public DateTimeOffset? TimeStamp { get; set; }

        /// <summary>
        /// Gets or sets a dictionary for any additional CloudEvents attributes not explicitly defined in this class.
        /// Uses the <see cref="JsonExtensionDataAttribute"/> for serialization and deserialization of these properties.
        /// </summary>
        [JsonPropertyName("headers")]
        public IDictionary<string, object>? Headers { get; set; }

        /// <summary>
        /// Gets or sets the event data payload as a <see cref="JsonElement"/>.
        /// Complies with the 'data' attribute.
        /// This allows for deferred deserialization or direct manipulation of the JSON data.
        /// </summary>
        [JsonPropertyName("data")]
        public JsonElement? Data { get; set; }

        /// <summary>
        /// Used for binary data in CloudEvents.
        /// </summary>
        [JsonPropertyName("data_base64")]
        public string? DataBase64 { get; set; }
    }

    internal static partial class Log
    {
        [LoggerMessage(LogLevel.Error, "Error during deserialization a JSON on wrap")]
        public static partial void ErrorDuringDeserializerAJsonOnWrap(ILogger logger, Exception ex);

        [LoggerMessage(LogLevel.Error, "Error during deserialization a Cloud Event JSON on unwrap")]
        public static partial void ErrorDuringDeserializerOnUnwrap(ILogger logger, Exception ex);
        
        
        [LoggerMessage(LogLevel.Error, "Error during deserialization a XML on wrap")]
        public static partial void ErrorDuringDeserializerAXMlOnWrap(ILogger logger, Exception ex);

        [LoggerMessage(LogLevel.Error, "Error during deserialization a XML Event JSON on unwrap")]
        public static partial void ErrorDuringXMLDeserializerOnUnwrap(ILogger logger, Exception ex);
    }
}
