﻿using System;
using Paramore.Brighter;

namespace GreetingsApp.Requests
{
    public class DeletePerson : Command
    {
        public string Name { get; }

        public DeletePerson(string name) 
            : base(Guid.NewGuid())
        {
            Name = name;
        }
    }
}
