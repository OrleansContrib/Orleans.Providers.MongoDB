# Orleans.Providers.MongoDB

![Nuget](https://img.shields.io/nuget/v/Orleans.Providers.MongoDB)

> Feedback would be appreciated.

A MongoDb implementation of the Orleans Providers. 

This includes

 * Membership (`IMembershipTable` & `IGatewayListProvider`)
 * Reminders (`IReminderTable`)
 * Storage (`IStoragePRovider`)

## Installation

```ps
install-package Orleans.Providers.MongoDB
```

## Setup

### Install MongoDB (BREAKING CHANGE)
 
From 3.1 onwards you have to register the IMongoClient in the service locator.

```csharp
### Server side

# via Silo Host Builder (ISiloHostBuilder)
var silo = new SiloHostBuilder()
    .UseMongoDBClient("mongodb://localhost")
    ...
    .Build();

# via Generic Host + Silo Builder (ISiloBuilder)
var host = Host.CreateDefaultBuilder(args)
	.UseOrleans((context, siloBuilder) => {
		siloBuilder.UseMongoDBClient("mongodb://localhost")
		...
	});

### Client side
var client = new ClientBuilder()
    .UseMongoDBClient("mongodb://localhost")
    ...
    .Build();
```

as an alternative you can also implement the IMongoClientFactory interface and override the client names with options.

There is also an overload of ```UseMongoDBClient()``` that takes a ```Func<IServiceProvider, MongoClientSettings>``` which allows you specify all MongoDB connection settings individually. This is especially useful, if you need to separate network settings and credentials into different configuration variables, or if you want to bind to an ```IOptions<T>``` configuration as shown below.

```csharp
[...]
  .UseMongoDBClient(provider =>
    {
      var cfg = provider.GetRequiredService<IOptions<MyMongoDbConfiguration>>();

      var settings = MongoClientSettings.FromConnectionString(cfg.Value.ConnectionString);
      settings.Credential = MongoCredential.CreateCredential(cfg.Value.AuthDatabase, cfg.Value.UserName, cfg.Value.Password);

      return settings;
    })
[...]
```




### Membership

Use the client builder to setup mongo db:

```csharp
var client = new ClientBuilder()
    .UseMongoDBClustering(options =>
    {
        options.DatabaseName = dbName;
        options.Strategy = MongoDBMembershipStrategy.Multiple;
    })
    ...
    .Build();
```

and the same for the silo builder:

```csharp
var silo = new SiloHostBuilder()
     .UseMongoDBClustering(options =>
    {
        options.DatabaseName = dbName;
        options.Strategy = MongoDBMembershipStrategy.Multiple;
    })
    ...
    .Build();
```

The provider supports three different strategies for membership management:

1. ```SingleDocument```: A single document per deployment. Fastest for small clusters.
2. ```Multiple```: One document per silo and an extra document for the table version. Needs a replica set and transaction support to work properly.
3. ```MultipleDeprecated```: One document per silo but no support for the extended membership protocol. Not recommended.

### Reminders

Just use the silo builder:

```csharp
var silo = new SiloHostBuilder()
    .UseMongoDBReminders(options =>
    {
        options.DatabaseName = dbName;
        options.CreateShardKeyForCosmos = createShardKey;
    })
    ...
    .Build();
```

### Storage

Just use the silo builder:

```csharp
var silo = new SiloHostBuilder()
    .AddMongoDBGrainStorage("grains-storage-provider-name",
    options =>
    {
        options.DatabaseName = dbName;
        options.CreateShardKeyForCosmos = createShardKey;
    })
    ...
    .Build();
```

### Storage Serializers
Mongo provider supports the following serializers:
* `JsonGrainStateSerializer (Default)`: uses Newtonsoft.JSON
* `BinaryGrainStateSerializer`: uses Orleans binary serializer
* `BsonGrainStateSerializer`: uses [BsonSerializer](https://mongodb.github.io/mongo-csharp-driver/2.18/reference/bson/serialization/)

### How to configure `JsonGrainStateSerializer`
```csharp
services.Configure<JsonGrainStateSerializerOptions>(options =>
{
    options.ConfigureJsonSerializerSettings = settings =>
    {
        settings.NullValueHandling = NullValueHandling.Include;
        settings.DefaultValueHandling = DefaultValueHandling.Populate;
        settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
    });
});
```

### How to change default serializer
For example, in order to change the default from `JsonGrainStateSerializer` to `BsonGrainStateSerializer`
```csharp
services.AddSingleton<IGrainStateSerializer, BsonGrainStateSerializer>();
```

### How to configure serializer for a specific storage provider
For example in order to use binary serializer for **PubSubStore**
```csharp
services.AddSingletonNamedService<IGrainStateSerializer, BinaryGrainStateSerializer>("PubSubStore");
...
var silo = new SiloHostBuilder()
    .AddMongoDBGrainStorage("PubSubStore", options => options.DatabaseName = dbName)
    ...
    .Build();
```

### Configuring a MongoDB Collection

At times during storage setup, it may be necessary to configure a specific collection where a certain type of Orleans grains is stored. The most common scenario involves adding additional indexes to certain fields, which allows for traditional querying of the grain collection.

**Note: For performance reasons, Orleans.Providers.MongoDB configures the default index (_id) to the Orleans grain Id.**

#### Example
In the example below, we're adding two additional indexes to the `EmailAddress` and `Username` fields of the Orleans grain state.

**Note: The delegate `CollectionSetupConfigurator` is invoked for every collection in the defined storage.** As you can see in the example, `collection.CollectionNamespace.CollectionName` is used to filter which collection should receive the additional indexes.

```csharp
// UserGrain State Storage Setup
.AddMongoDBGrainStorage(name: "UserStorage", options =>
{
    options.DatabaseName = "UsersDb";
    options.CreateShardKeyForCosmos = false;
    options.CollectionPrefix = string.Empty; // Don not add default 'Grains' prefix to collections
    options.CollectionSetupConfigurator = (collection =>
    {
        // IMPORTANT!
        // CollectionSetupConfigurator delegate will be called for each collection that is created by Orleans MongoDB storage provider
        // Use CollectionNamespace.CollectionName to determine the collection name and apply indexing only on the collections that you need

        // EXAMPLE --------------------------------------------------------------------------------
        // Only apply indexing on the UserGrain collection
        if(collection.CollectionNamespace.CollectionName == "UserModel")
        { 
            // Create additional indexes on a collection
            // --------------------------------------------------------------------------------------
            // Note: Orleans MongoDB storage provider will create index on the grain id automatically
            // Note: Orleans MongoDB storage provider persists the grain state in a sub-document named '_doc' (see in Mongo Atlas)

            // Create an index on the EmailAddress field
            var indexOptions = new CreateIndexOptions
            {
                Name = "_emailAddress_",
                Unique = true
            };
            var emailIndexModel = new CreateIndexModel<BsonDocument>(new BsonDocument("_doc.EmailAddress", 1), indexOptions);
            collection.Indexes.CreateOne(emailIndexModel);

            // Create an index on the Username field
            var usernameIndexOptions = new CreateIndexOptions
            {
                Name = "_username_",
                Unique = true
            };
            var usernameIndexModel = new CreateIndexModel<BsonDocument>(new BsonDocument("_doc.Username", 1), usernameIndexOptions);
            collection.Indexes.CreateOne(usernameIndexModel);
        }

    });
});
```

## Remarks

As you can see you have to pass in the connection string to each provider. But we will only create one MongoDB client for each unique connection string to keep the number of connections to your cluster or server as low as possible.


## Building the unit tests

In order to make use of many tests already defined in [Orleans](https://github.com/dotnet/orleans/), the [unit test project of this module](https://github.com/OrleansContrib/Orleans.Providers.MongoDB/tree/master/UnitTest) depends on [TesterInternal](https://github.com/dotnet/orleans/tree/main/test/TesterInternal), which is added as a project reference from the local path [./libs](https://github.com/OrleansContrib/Orleans.Providers.MongoDB/tree/master/libs) where the whole Orleans source code is mirrored as a [git submodule](https://git-scm.com/docs/git-submodule).

This comes with two caveats:
* Depending on your git client, the submodules are sometimes not pulled automatically. If you find the ./libs subdirectory to be empty, execute ```git pull --recurse-submodules``` manually from the command shell
* some of the projects in the ./libs subfolder need F# support to be present in your VisualStudio installation, otherwise you will receive an error message about an unsupported language. This may apply to other IDEs as well