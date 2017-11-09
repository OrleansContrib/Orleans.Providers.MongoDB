using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using Orleans.Providers.MongoDB.StorageProviders;
using Xunit;
using Newtonsoft.Json.Linq;
using Orleans.Storage;

namespace Orleans.Providers.MongoDB.UnitTest.Storage
{
    public class MongoDataManagerTests
    {
        private readonly MongoDataManager sut = new MongoDataManager("OrleansTest", "mongodb://localhost");
        private readonly string dataId = Guid.NewGuid().ToString();
        private readonly string dataType = "Test";

        public class Counter
        {
            public int Value { get; set; }
        }

        [Fact]
        public async Task Should_read_from_old_version()
        {
            sut.Database.GetCollection<BsonDocument>(dataType).InsertOne(
                new BsonDocument
                {
                    ["_id"] = dataId,
                    ["Value"] = 123,
                });

            var doc = await sut.Read(dataType, dataId);
            var obj = doc.Value.ToObject<Counter>();

            Assert.Equal(123, obj.Value);
        }

        [Fact]
        public async Task Should_throw_exception_if_etag_does_not_match()
        {
            var data = JObject.FromObject(new Counter { Value = 500 });

            await sut.Write(dataType, dataId, data, "TAG");

            await Assert.ThrowsAsync<InconsistentStateException>(() => sut.Write(dataType, dataId, data, "TAG"));
        }

        [Fact]
        public async Task Should_overwrite_old_format_and_assign_etag()
        {
            sut.Database.GetCollection<BsonDocument>(dataType).InsertOne(
                new BsonDocument
                {
                    ["_id"] = dataId,
                    ["Value"] = 123,
                });

            await sut.Write(dataType, dataId, JObject.FromObject(new Counter { Value = 500 }), "TAG");

            var result = (await sut.Read(dataType, dataId));

            var etag = result.Etag;
            var data = result.Value.ToObject<Counter>();

            Assert.Equal(500, data.Value);
            Assert.NotNull(etag);
        }

        [Fact]
        public async Task Should_serialize_in_sequence()
        {
            var data = new Counter { Value = 1 };
            var etag = string.Empty;

            for (var i = 0; i < 10; i++)
            {
                await sut.Write(dataType, dataId, JObject.FromObject(data), etag);

                var result = (await sut.Read(dataType, dataId));

                etag = result.Etag;
                data = result.Value.ToObject<Counter>();

                data.Value++;
            }

            Assert.Equal(11, data.Value);
        }
    }
}
