﻿using System.Text.Json;
using MongoDB.Bson.Serialization.Attributes;

namespace Paramore.Brighter.Outbox.MongoDb;

public class MessageItem
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="expiresAt"></param>
    public MessageItem(Message message, long? expiresAt = null)
    {
        var date = message.Header.TimeStamp == DateTimeOffset.MinValue
            ? DateTimeOffset.UtcNow
            : message.Header.TimeStamp;

        Body = message.Body.Bytes;
        ContentType = message.Header.ContentType;
        CorrelationId = message.Header.CorrelationId.ToString();
        CharacterEncoding = message.Body.CharacterEncoding.ToString();
        CreatedAt = date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        CreatedTime = date.Ticks;
        OutstandingCreatedTime = date.Ticks;
        DeliveryTime = null;
        HeaderBag = JsonSerializer.Serialize(message.Header.Bag, JsonSerialisationOptions.Options);
        MessageId = message.Id;
        MessageType = message.Header.MessageType.ToString();
        PartitionKey = message.Header.PartitionKey;
        ReplyTo = message.Header.ReplyTo;
        Topic = message.Header.Topic;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// The message body
    /// </summary>
    public byte[] Body { get; set; }

    /// <summary>
    /// What is the character encoding of the body
    /// </summary>
    public string? CharacterEncoding { get; set; }

    /// <summary>
    /// What is the content type of the message
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// The correlation id of the message
    /// </summary>
    public string CorrelationId { get; set; }

    /// <summary>
    /// The time at which the message was created, formatted as a string yyyy-MM-ddTHH:mm:ss.fffZ
    /// </summary>
    public string CreatedAt { get; set; }

    /// <summary>
    /// The time at which the message was created, in ticks
    /// </summary>
    public long CreatedTime { get; set; }

    /// <summary>
    /// The time at which the message was created, in ticks. Null if the message has been dispatched.
    /// </summary>
    public long? OutstandingCreatedTime { get; set; }

    /// <summary>
    /// The time at which the message was delivered, formatted as a string yyyy-MM-dd
    /// </summary>
    public DateTimeOffset? DeliveredAt { get; set; }

    /// <summary>
    /// The time that the message was delivered to the broker, in ticks
    /// </summary>
    public long? DeliveryTime { get; set; }

    /// <summary>
    /// A JSON object representing a dictionary of additional properties set on the message
    /// </summary>
    public string HeaderBag { get; set; }

    /// <summary>
    /// The Id of the Message. Used as a Global Secondary Index
    /// </summary>
    [BsonId]
    public string MessageId { get; set; }

    /// <summary>
    /// The type of message i.e. MT_COMMAND, MT_EVENT etc. An enumeration rendered as a string
    /// </summary>
    public string MessageType { get; set; }

    /// <summary>
    /// The partition key for the Kafka message
    /// </summary>
    public string PartitionKey { get; set; }


    /// <summary>
    /// If this is a conversation i.e. request-response, what is the reply channel
    /// </summary>
    public string? ReplyTo { get; set; }

    /// <summary>
    /// The Topic the message was published to
    /// </summary>
    /// 
    public string Topic { get; set; }

    public long? ExpiresAt { get; set; }

    public Message ConvertToMessage()
    {
        //following type may be missing on older data
        var characterEncoding = CharacterEncoding != null
            ? (CharacterEncoding)Enum.Parse(typeof(CharacterEncoding), CharacterEncoding)
            : Brighter.CharacterEncoding.UTF8;
        var correlationId = CorrelationId;
        var messageId = MessageId;
        var messageType = (MessageType)Enum.Parse(typeof(MessageType), MessageType);
        var timestamp = new DateTime(CreatedTime, DateTimeKind.Utc);

        var header = new MessageHeader(
            messageId: messageId,
            topic: new RoutingKey(Topic),
            messageType: messageType,
            timeStamp: timestamp,
            correlationId: correlationId,
            replyTo: ReplyTo == null ? RoutingKey.Empty : new RoutingKey(ReplyTo),
            contentType: ContentType ?? MessageBody.APPLICATION_JSON,
            partitionKey: PartitionKey);

        var bag = JsonSerializer.Deserialize<Dictionary<string, object>>(HeaderBag, JsonSerialisationOptions.Options)!;
        foreach (var key in bag.Keys)
        {
            header.Bag.Add(key, bag[key]);
        }

        var body = new MessageBody(Body, ContentType ?? MessageBody.APPLICATION_JSON, characterEncoding);

        return new Message(header, body);
    }
}
