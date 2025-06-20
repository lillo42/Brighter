#region Licence
/* The MIT License (MIT)
Copyright © 2014 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

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
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Annotations;
using Paramore.Brighter.JsonConverters;
using Paramore.Brighter.NJsonConverters;

namespace Paramore.Brighter;

/// <summary>
/// A command is an imperative instruction to do something. We expect only one receiver of a command because it is point-to-point
/// </summary>
public class Command : ICommand
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    /// <value>The identifier.</value>
    [NotNull]
    [JsonConverter(typeof(IdConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(NIdConverter))]
    [Display(Name = "id", Description = "The unique identifier for the command")]
    [JsonSchema(JsonObjectType.String)]
    public Id Id { get; set; }
        
    /// <summary>
    /// Initializes a new instance of the <see cref="Command"/> class.
    /// </summary>
    /// <param name="id">The identifier.</param>
    public Command(Id id)
    {
        Id = id;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Command"/> class. 
    /// </summary>
    /// <param name="id">The identifier</param>
    public Command(Guid id)
    {
        Id = new Id(id.ToString()); 
    }
}
