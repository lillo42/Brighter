﻿using MongoDB.Driver;
using Paramore.Brighter.Observability;

namespace Paramore.Brighter.Inbox.MongoDb;

/// <summary>
/// The MongoDB configuration
/// </summary>
public class MongoDbInboxConfiguration
{
    /// <summary>
    /// Initialize new instance of <see cref="MongoDbInboxConfiguration"/>
    /// </summary>
    /// <param name="connectionString">The Mongo db connection string.</param>
    /// <param name="databaseName">The database name.</param>
    /// <param name="collectionName">The collection name.</param>
    public MongoDbInboxConfiguration(string connectionString, string databaseName, string? collectionName = null)
    {
        ConnectionString = connectionString;
        DatabaseName = databaseName;
        CollectionName = collectionName ?? "brighter_inbox";
        Client = new MongoClient(connectionString);
    }
    
    
    /// <summary>
    /// The <see cref="MongoClient"/>
    /// </summary>
    public MongoClient Client { get; set; }

    /// <summary>
    /// The mongo db connection string
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    /// The mongodb database name
    /// </summary>
    public string DatabaseName { get; }

    /// <summary>
    /// The mongodb collection
    /// </summary>
    public string CollectionName { get; }
    
    /// <summary>
    /// The <see cref="System.TimeProvider"/>
    /// </summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;
    
    /// <summary>
    /// Action to be performed when it's resolving a collection  
    /// </summary>
    public OnResolvingAInboxCollection MakeCollection { get; set; } = OnResolvingAInboxCollection.Assume;

    /// <summary>
    /// The <see cref="MongoDatabaseSettings"/> used when access the database.
    /// </summary>
    public MongoDatabaseSettings? DatabaseSettings { get; set; }

    /// <summary>
    /// The <see cref="MongoDatabaseSettings"/> used to get collection
    /// </summary>
    public MongoCollectionSettings? CollectionSettings { get; set; }

    /// <summary>
    /// The <see cref="CreateCollectionOptions"/>.
    /// </summary>
    public CreateCollectionOptions? CreateCollectionOptions { get; set; }

    /// <summary>
    /// The <see cref="InstrumentationOptions"/>.
    /// </summary>
    public InstrumentationOptions InstrumentationOptions { get; set; } = InstrumentationOptions.All;
}
