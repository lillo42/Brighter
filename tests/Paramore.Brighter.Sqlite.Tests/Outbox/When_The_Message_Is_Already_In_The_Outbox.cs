#region Licence

/* The MIT License (MIT)
Copyright © 2014 Francesco Pighi <francesco.pighi@gmail.com>

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
using System.Threading.Tasks;
using Paramore.Brighter.Outbox.Sqlite;
using Xunit;

namespace Paramore.Brighter.Sqlite.Tests.Outbox
{
    [Trait("Category", "Sqlite")]
    public class SqliteOutboxMessageAlreadyExistsTests : IAsyncDisposable
    {
        private readonly SqliteTestHelper _sqliteTestHelper;
        private readonly SqliteOutbox _sqlOutbox;
        private Exception _exception;
        private readonly Message _messageEarliest;

        public SqliteOutboxMessageAlreadyExistsTests()
        {
            _sqliteTestHelper = new SqliteTestHelper();
            _sqliteTestHelper.SetupMessageDb();
            _sqlOutbox = new SqliteOutbox(_sqliteTestHelper.OutboxConfiguration);
            _messageEarliest = new Message(new MessageHeader(Guid.NewGuid().ToString(), new RoutingKey("test_topic"), MessageType.MT_DOCUMENT),
                new MessageBody("message body"));
            _sqlOutbox.Add(_messageEarliest, new RequestContext());
        }

        [Fact]
        public void When_The_Message_Is_Already_In_The_Outbox()
        {
            _exception = Catch.Exception(() => _sqlOutbox.Add(_messageEarliest, new RequestContext()));

            //should ignore the duplicate key and still succeed
            Assert.Null(_exception);
        }

        public async ValueTask DisposeAsync()
        {
            await _sqliteTestHelper.CleanUpDbAsync();
        }
    }
}
