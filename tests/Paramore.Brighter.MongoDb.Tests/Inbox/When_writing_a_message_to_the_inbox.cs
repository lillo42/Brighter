﻿#region Licence

/* The MIT License (MIT)
Copyright © 2020 Ian Cooper <ian.cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the “Software”), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

#endregion


using System;
using Paramore.Brighter.Inbox.Exceptions;
using Paramore.Brighter.Inbox.MongoDb;
using Paramore.Brighter.MongoDb.Tests.TestDoubles;
using Xunit;

namespace Paramore.Brighter.MongoDb.Tests.Inbox;

[Trait("Category", "MongoDb")]
public class MongoDbInboxAddMessageTests : IDisposable
{
    private readonly string _collection;
    private readonly MongoDbInbox _inbox;
    private readonly MyCommand _raisedCommand;
    private readonly string _contextKey;

    public MongoDbInboxAddMessageTests()
    {
        _collection = $"inbox-{Guid.NewGuid():N}";
        _inbox = new MongoDbInbox(Configuration.CreateInbox(_collection));

        _raisedCommand = new MyCommand { Value = "Test" };
        _contextKey = "context-key";
        _inbox.Add(_raisedCommand, _contextKey, null);
    }

    [Fact]
    public void When_Writing_A_Message_To_The_Inbox()
    {
        var storedCommand = _inbox.Get<MyCommand>(_raisedCommand.Id, _contextKey, null);

        //_should_read_the_command_from_the__sql_inbox
        Assert.NotNull(storedCommand);
        //_should_read_the_command_value
        Assert.Equal(_raisedCommand.Value, storedCommand.Value);
        //_should_read_the_command_id
        Assert.Equal(_raisedCommand.Id, storedCommand.Id);
    }

    [Fact]
    public void When_Reading_A_Message_From_The_Inbox_And_ContextKey_IsNull()
    {
        var exception = Catch.Exception(() => _ = _inbox.Get<MyCommand>(_raisedCommand.Id, null, null));
        //should_not_read_message
        Assert.IsType<RequestNotFoundException<MyCommand>>(exception);
    }

    public void Dispose()
    {
        Configuration.Cleanup(_collection);
    }
}
