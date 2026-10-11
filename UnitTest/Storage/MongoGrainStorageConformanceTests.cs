using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.Persistence.TestKit;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Providers.MongoDB.UnitTest.Fixtures;
using TestExtensions;
using Xunit;

namespace Orleans.Providers.MongoDB.UnitTest.Storage
{
    public abstract class MongoGrainStorageFixture : GrainStorageTestFixture, IAsyncLifetime
    {
        protected override string StorageProviderName => "MongoStorage";

        protected override void ConfigureSilo(ISiloBuilder siloBuilder)
        {
            ConfigureSerializer(siloBuilder.Services);

            siloBuilder
                .UseMongoDBClient(MongoDatabaseFixture.DatabaseConnectionString)
                .AddMongoDBGrainStorage(StorageProviderName, options => options.DatabaseName = $"OrleansTestStorage_{GetType().Name}");
        }

        protected virtual void ConfigureSerializer(IServiceCollection services)
        {
        }
    }

    public sealed class MongoGrainStorageFixture_Json : MongoGrainStorageFixture
    {
    }

    public sealed class MongoGrainStorageFixture_Binary : MongoGrainStorageFixture
    {
        protected override void ConfigureSerializer(IServiceCollection services)
        {
            services.AddKeyedSingleton<IGrainStateSerializer, BinaryGrainStateSerializer>(StorageProviderName);
        }
    }

    public sealed class MongoGrainStorageFixture_Bson : MongoGrainStorageFixture
    {
        protected override void ConfigureSerializer(IServiceCollection services)
        {
            services.AddKeyedSingleton<IGrainStateSerializer, BsonGrainStateSerializer>(StorageProviderName);
        }
    }

    [TestCategory("Persistence")]
    [TestCategory("Mongo")]
    public abstract class MongoGrainStorageConformanceTests : GrainStorageTestRunner
    {
        protected MongoGrainStorageConformanceTests(MongoGrainStorageFixture fixture)
            : base(fixture.Storage)
        {
        }

        [Fact]
        public override Task PersistenceStorage_WriteReadIdCyrillic() => base.PersistenceStorage_WriteReadIdCyrillic();

        [Fact]
        public override Task PersistenceStorage_WriteDuplicateFailsWithInconsistentStateException() => base.PersistenceStorage_WriteDuplicateFailsWithInconsistentStateException();

        [Fact]
        public override Task PersistenceStorage_WriteInconsistentFailsWithInconsistentStateException() => base.PersistenceStorage_WriteInconsistentFailsWithInconsistentStateException();

        [Fact]
        public override Task PersistenceStorage_WriteReadWriteReadStatesInParallel() => base.PersistenceStorage_WriteReadWriteReadStatesInParallel();

        [Fact]
        public override Task PersistenceStorage_ReadNonExistentState() => base.PersistenceStorage_ReadNonExistentState();

        [Fact]
        public override Task PersistenceStorage_WriteClearWrite() => base.PersistenceStorage_WriteClearWrite();

        [Fact]
        public override Task PersistenceStorage_WriteClearRead() => base.PersistenceStorage_WriteClearRead();

        [Fact]
        public override Task PersistenceStorage_WriteRead_StringKey() => base.PersistenceStorage_WriteRead_StringKey();

        [Fact]
        public override Task PersistenceStorage_WriteRead_IntegerKey() => base.PersistenceStorage_WriteRead_IntegerKey();

        [Fact]
        public override Task PersistenceStorage_ETagChangesOnWrite() => base.PersistenceStorage_ETagChangesOnWrite();

        [Fact]
        public override Task PersistenceStorage_ClearBeforeWrite() => base.PersistenceStorage_ClearBeforeWrite();

        [Fact]
        public override Task PersistenceStorage_ClearStateDoesNotNullifyState() => base.PersistenceStorage_ClearStateDoesNotNullifyState();

        [Fact]
        public override Task PersistenceStorage_ClearUpdatesETag() => base.PersistenceStorage_ClearUpdatesETag();

        [Fact]
        public override Task PersistenceStorage_ReadAfterClear() => base.PersistenceStorage_ReadAfterClear();

        [Fact]
        public override Task PersistenceStorage_MultipleClearOperations() => base.PersistenceStorage_MultipleClearOperations();

        [Fact]
        public override Task PersistenceStorage_ReadNonExistentStateHasNonNullState() => base.PersistenceStorage_ReadNonExistentStateHasNonNullState();

        [Fact]
        public override Task PersistenceStorage_StateNamesUseIndependentRecords() => base.PersistenceStorage_StateNamesUseIndependentRecords();

        [Fact]
        public override Task PersistenceStorage_ClearInconsistentFailsWithInconsistentStateException() => base.PersistenceStorage_ClearInconsistentFailsWithInconsistentStateException();

        [Fact]
        public override Task PersistenceStorage_WriteReadClearReadCycle() => base.PersistenceStorage_WriteReadClearReadCycle();

        [Fact]
        public override Task PersistenceStorage_WriteWithSameValuesUpdatesETag() => base.PersistenceStorage_WriteWithSameValuesUpdatesETag();

        [Fact, TestCategory("ModelBased")]
        public Task PersistenceStorage_ModelBasedGeneratedConformance()
        {
            return new GrainStorageModelBasedTestRunner(Storage, GetType().Name).RunGeneratedConformanceTests(TestContext.Current.CancellationToken);
        }
    }

    public sealed class MongoGrainStorageConformanceTests_Json : MongoGrainStorageConformanceTests, IClassFixture<MongoGrainStorageFixture_Json>
    {
        public MongoGrainStorageConformanceTests_Json(MongoGrainStorageFixture_Json fixture)
            : base(fixture)
        {
        }
    }

    public sealed class MongoGrainStorageConformanceTests_Binary : MongoGrainStorageConformanceTests, IClassFixture<MongoGrainStorageFixture_Binary>
    {
        public MongoGrainStorageConformanceTests_Binary(MongoGrainStorageFixture_Binary fixture)
            : base(fixture)
        {
        }
    }

    public sealed class MongoGrainStorageConformanceTests_Bson : MongoGrainStorageConformanceTests, IClassFixture<MongoGrainStorageFixture_Bson>
    {
        public MongoGrainStorageConformanceTests_Bson(MongoGrainStorageFixture_Bson fixture)
            : base(fixture)
        {
        }
    }
}
