﻿using System;
using Paramore.Brighter.Gcp.Tests.Helper;

namespace Paramore.Brighter.Gcp.Tests.TestDoubles;

public class MyLargeCommand(int valueLength) : Command(Guid.NewGuid())
{
    public string Value { get; set; } = DataGenerator.CreateString(valueLength);

    public MyLargeCommand() : this(0) { /* requires a default constructor to deserialize*/ }
}
