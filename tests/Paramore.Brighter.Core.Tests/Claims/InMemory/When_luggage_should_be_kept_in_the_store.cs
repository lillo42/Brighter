﻿using System;
using System.IO;
using Paramore.Brighter.Core.Tests.TestHelpers;
using Paramore.Brighter.Transforms.Storage;
using Paramore.Brighter.Transforms.Transformers;
using Xunit;

namespace Paramore.Brighter.Core.Tests.Claims.InMemory;

public class RetrieveClaimLeaveLuggage
{
    private readonly InMemoryStorageProvider _store;
    private readonly ClaimCheckTransformer _transformer;
    private readonly string _contents;

    public RetrieveClaimLeaveLuggage()
    {
        _store = new InMemoryStorageProvider();
        _transformer = new ClaimCheckTransformer(_store, _store);
        _transformer.InitializeUnwrapFromAttributeParams(true);

        _contents = DataGenerator.CreateString(6000);
    }

    [Fact]
    public void When_luggage_should_be_kept_in_the_store()
    {
        //arrange
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.WriteAsync(_contents);
        writer.FlushAsync();
        stream.Position = 0;

        var id = _store.Store(stream);

        var message = new Message(
            new MessageHeader(Guid.NewGuid().ToString(), new("test_topic"), MessageType.MT_EVENT, timeStamp: DateTime.UtcNow),
            new MessageBody($"Claim Check {id}"));
        message.Header.DataRef = id;
        message.Header.Bag[ClaimCheckTransformer.CLAIM_CHECK] = id;

        //act
        _ = _transformer.Unwrap(message);

        //assert
        bool hasLuggage = message.Header.Bag.TryGetValue(ClaimCheckTransformer.CLAIM_CHECK, out object _);
        Assert.True(hasLuggage);
        Assert.True(_store.HasClaim(id));
    }
}
