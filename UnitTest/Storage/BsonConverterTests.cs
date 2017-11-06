using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using FluentAssertions;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    [TestClass]
    public class BsonConverterTests
    {
        public class TestObject
        {
            public TimeSpan TimeSpan { get; set; }

            public Guid Guid { get; set; }

            public DateTimeOffset DateTimeOffset { get; set; }

            public DateTime DateTime { get; set; }

            public Int64 Int64 { get; set; }

            public Int32 Int32 { get; set; }

            public Int16 Int16 { get; set; }

            public UInt64 UInt64 { get; set; }

            public UInt32 UInt32 { get; set; }

            public UInt16 UInt16 { get; set; }

            public double Float64 { get; set; }

            public float Float32 { get; set; }

            public bool Bool { get; set; }

            public byte Byte { get; set; }

            public byte[] Bytes { get; set; }

            public string String { get; set; }

            public string[] Strings { get; set; }

            public Uri Uri { get; set; }

            public static TestObject CreateWithValues()
            {
                return new TestObject
                {
                    Bool = true,
                    Byte = 0x2,
                    Bytes = new byte[] { 0x10, 0x12, 0x13 },
                    DateTimeOffset = DateTime.Today,
                    DateTime = DateTime.Today,
                    Float32 = 32.5f,
                    Float64 = 32.5d,
                    Guid = Guid.NewGuid(),
                    Int64 = 64,
                    Int32 = 32,
                    Int16 = 16,
                    String = "orleans",
                    Strings = new[] { "hello", "orleans " },
                    TimeSpan = TimeSpan.FromSeconds(123),
                    UInt64 = 164,
                    UInt32 = 132,
                    UInt16 = 116,
                    Uri = new Uri("http://orleans.org")
                };
            }
        }

        [TestMethod]
        public void Should_serialize_and_deserialize_to_bson_with_json()
        {
            var source = TestObject.CreateWithValues();
            var target = JObject.FromObject(source).ToBson().ToJToken().ToObject<TestObject>();

            target.ShouldBeEquivalentTo(source);
        }
    }
}
