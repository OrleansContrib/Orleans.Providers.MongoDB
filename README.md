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

### Membership

Use the client builder to setup mongo db:

```csharp
var client = new ClientBuilder()
    .UseMongoDBClustering(options =>
    {
        options.DatabaseName = dbName;
        options.Strategy = MongoDBMembershipStrategy.Muiltiple
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
        options.Strategy = MongoDBMembershipStrategy.Muiltiple
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
    .AddMongoDBGrainStorage("grains-storage-provider-name"
    options =>
    {
        options.DatabaseName = dbName;
        options.CreateShardKeyForCosmos = createShardKey;

        options.ConfigureJsonSerializerSettings = settings =>
        {
            settings.NullValueHandling = NullValueHandling.Include;
            settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
            settings.DefaultValueHandling = DefaultValueHandling.Populate;
        };
    })
    ...
    .Build();
```

The provider uses Newtonsoft.JSON to serialize and deserialize MongoDB documents. In our benchmarks we have noticed that is slightly faster and has the benfit that you do not need to care about two serializers.

## Remarks

As you can see you have to pass in the connection string to each provider. But we will only create one MongoDB client for each unique connection string to keep the number of connections to your cluster or server as low as possible.
