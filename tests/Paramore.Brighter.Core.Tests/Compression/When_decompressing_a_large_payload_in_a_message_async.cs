﻿using System;
using System.IO;
using System.IO.Compression;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Paramore.Brighter.Core.Tests.TestHelpers;
using Paramore.Brighter.Extensions;
using Paramore.Brighter.Transforms.Transformers;
using Xunit;

namespace Paramore.Brighter.Core.Tests.Compression;

public class AsyncUncompressLargePayloadTests
{
    private readonly RoutingKey _routingKey = new RoutingKey("test_topic");

    [Fact]
    public async Task When_decompressing_a_large_gzip_payload_in_a_message()
    {
        //arrange
        var transformer = new CompressPayloadTransformer();
        transformer.InitializeUnwrapFromAttributeParams(CompressionMethod.GZip);
        
        var largeContent = DataGenerator.CreateString(6000);
        
        using var input = new MemoryStream(Encoding.ASCII.GetBytes(largeContent));
        using var output = new MemoryStream();

        Stream compressionStream = new GZipStream(output, CompressionLevel.Optimal);
            
        string mimeType = CompressPayloadTransformer.GZIP;
        await input.CopyToAsync(compressionStream);
        await compressionStream.FlushAsync();

        var body = new MessageBody(output.ToArray(), new ContentType(mimeType));
        
        var message = new Message(
            new MessageHeader(Guid.NewGuid().ToString(), _routingKey, MessageType.MT_EVENT, 
                timeStamp:DateTime.UtcNow, contentType: new ContentType(mimeType)
            ),
            body
        );
        
        message.Header.Bag[CompressPayloadTransformer.ORIGINAL_CONTENTTYPE_HEADER] = MediaTypeNames.Application.Json;
        
        //act
        var msg = await transformer.UnwrapAsync(message);
  
        //assert
        Assert.Equal(largeContent, msg.Body.Value);
        var expected = new ContentType(MediaTypeNames.Application.Json){CharSet = CharacterEncoding.UTF8.FromCharacterEncoding()};
        Assert.Equal(expected, msg.Body.ContentType);
        Assert.Equal(expected, msg.Header.ContentType);
    }
    
    [Fact]
    public async Task When_decompressing_a_large_zlib_payload_in_a_message()
    {
        //arrange
        var transformer = new CompressPayloadTransformer();
        transformer.InitializeUnwrapFromAttributeParams(CompressionMethod.Zlib);
        
        var largeContent = DataGenerator.CreateString(6000);
        
        using var input = new MemoryStream(Encoding.ASCII.GetBytes(largeContent));
        using var output = new MemoryStream();

        Stream compressionStream = new ZLibStream(output, CompressionLevel.Optimal);
            
        string mimeType = CompressPayloadTransformer.DEFLATE;
        await input.CopyToAsync(compressionStream);
        await compressionStream.FlushAsync();

        var body = new MessageBody(output.ToArray(), new ContentType(mimeType));
        
        var message = new Message(
            new MessageHeader(Guid.NewGuid().ToString(), _routingKey, MessageType.MT_EVENT, 
                timeStamp: DateTime.UtcNow, contentType: new ContentType(mimeType)
            ),
            body
        );
        
        message.Header.Bag[CompressPayloadTransformer.ORIGINAL_CONTENTTYPE_HEADER] = MediaTypeNames.Application.Json;
        
         //act
        var msg = await transformer.UnwrapAsync(message);
        
        //assert
        Assert.Equal(largeContent, msg.Body.Value);
        var expected = new ContentType(MediaTypeNames.Application.Json){CharSet = CharacterEncoding.UTF8.FromCharacterEncoding()};
        Assert.Equal(expected, msg.Body.ContentType);
        Assert.Equal(expected, msg.Header.ContentType);
    }
    
    [Fact]
    public async Task When_decompressing_a_large_brotli_payload_in_a_message()
    {
        //arrange
        var transformer = new CompressPayloadTransformer();
        transformer.InitializeUnwrapFromAttributeParams(CompressionMethod.Brotli);
        
        var largeContent = DataGenerator.CreateString(6000);
        
        using var input = new MemoryStream(Encoding.ASCII.GetBytes(largeContent));
        using var output = new MemoryStream();

        Stream compressionStream = new BrotliStream(output, CompressionLevel.Optimal);
            
        string mimeType = CompressPayloadTransformer.BROTLI;
        await input.CopyToAsync(compressionStream);
        await compressionStream.FlushAsync();

        var body = new MessageBody(output.ToArray(), new ContentType(mimeType));
        
        var message = new Message(
            new MessageHeader(Guid.NewGuid().ToString(), _routingKey, MessageType.MT_EVENT, 
                timeStamp: DateTime.UtcNow, contentType: new ContentType(mimeType)
            ),
            body
        );
        
        message.Header.Bag[CompressPayloadTransformer.ORIGINAL_CONTENTTYPE_HEADER] = MediaTypeNames.Application.Json;
        
        //act
         var msg = await transformer.UnwrapAsync(message);
 
         //assert
         Assert.Equal(largeContent, msg.Body.Value);
         var expected = new ContentType(MediaTypeNames.Application.Json){CharSet = CharacterEncoding.UTF8.FromCharacterEncoding()};
         Assert.Equal(expected, msg.Body.ContentType);
         Assert.Equal(expected, msg.Header.ContentType);
    }
}
