# Orleans.Providers.MongoDB

![Nuget](https://img.shields.io/nuget/v/Orleans.Providers.MongoDB)

> Feedback would be appreciated.

A MongoDb implementation of the Orleans Providers. 

This includes

 * Membership (`IMembershipTable` & `IGatewayListProvider`)
 * Reminders (`IReminderTable`)
 * Storage (`IStoragePRovider`)

The provider targets Orleans 10 on .NET 8 and .NET 10.

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
3. ```MultipleDeprecated```: One document per silo but no support for the extended membership protocol. Not recommended. This strategy does not pass the Orleans membership conformance suite because it does not maintain a table version.

`CleanupDefunctSiloEntries` only removes silos with status `Dead` whose last update (start time, heartbeat or suspicion vote) is older than the cutoff. Silos in any other status are never removed.

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
services.AddKeyedSingleton<IGrainStateSerializer, BinaryGrainStateSerializer>("PubSubStore");
...
var silo = new SiloHostBuilder()
    .AddMongoDBGrainStorage("PubSubStore", options => options.DatabaseName = dbName)
    ...
    .Build();
```

### Clearing state

`ClearStateAsync` deletes the stored document using optimistic concurrency: it throws an `InconsistentStateException` when the stored ETag does not match the ETag of the grain state. After clearing, the ETag is reset to `null` and the state is reset to a new instance.

## Remarks

As you can see you have to pass in the connection string to each provider. But we will only create one MongoDB client for each unique connection string to keep the number of connections to your cluster or server as low as possible.


## Building the unit tests

The unit tests run the provider conformance suites that Orleans uses for its own providers: the clustering, reminders and persistence TestKits ([Orleans.Clustering.TestKit](https://github.com/dotnet/orleans/tree/main/src/Orleans.Clustering.TestKit), [Orleans.Reminders.TestKit](https://github.com/dotnet/orleans/tree/main/src/Orleans.Reminders.TestKit), [Orleans.Persistence.TestKit](https://github.com/dotnet/orleans/tree/main/src/Orleans.Persistence.TestKit)) and the shared test base classes in [Orleans.Runtime.Internal.Tests](https://github.com/dotnet/orleans/tree/main/test/Orleans.Runtime.Internal.Tests). They are added as project references from the local path [./libs](https://github.com/OrleansContrib/Orleans.Providers.MongoDB/tree/master/libs) where the whole Orleans source code is mirrored as a [git submodule](https://git-scm.com/docs/git-submodule).

The tests use xUnit v3 with Microsoft.Testing.Platform and require the .NET 10 SDK (and the .NET 8 runtime to run the `net8.0` tests). A local MongoDB instance is started automatically with [MongoSandbox](https://github.com/wassim-k/MongoSandbox); use `-p:MongoVersion=6|7|8` to select the MongoDB version (default `8`):

```sh
dotnet test --project UnitTest/Orleans.Providers.MongoDB.UnitTest.csproj -p:MongoVersion=8
```

This comes with two caveats:
* Depending on your git client, the submodules are sometimes not pulled automatically. If you find the ./libs subdirectory to be empty, execute ```git pull --recurse-submodules``` manually from the command shell
* some of the projects in the ./libs subfolder need F# support to be present in your VisualStudio installation, otherwise you will receive an error message about an unsupported language. This may apply to other IDEs as well