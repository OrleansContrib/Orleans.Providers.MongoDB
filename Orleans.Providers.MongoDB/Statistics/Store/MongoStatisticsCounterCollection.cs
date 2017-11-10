using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Statistics.Store
{
    public class MongoStatisticsCounterCollection : CollectionBase<MongoStatisticsCounterDocument>
    {
        private static readonly InsertManyOptions NoValidation = new InsertManyOptions { BypassDocumentValidation = true };
        private readonly string collectionPrefix;

        public MongoStatisticsCounterCollection(string connectionString, string databaseName, string collectionPrefix)
            : base(connectionString, databaseName)
        {
            this.collectionPrefix = collectionPrefix;
        }

        protected override string CollectionName()
        {
            return collectionPrefix + "OrleansStatisticsTable";
        }

        public virtual Task InsertStatisticsCountersAsync(
            string deploymentId,
            string hostName,
            string name,
            string id, 
            List<ICounter> counterBatch)
        {
            var documents = new List<MongoStatisticsCounterDocument>();

            var now = DateTime.UtcNow;

            foreach (var counter in counterBatch)
            {
                var document = new MongoStatisticsCounterDocument
                {
                    DeploymentId = deploymentId,
                    HostName = hostName,
                    Name = name,
                    Identity = id,
                    IsValueDelta = counter.IsValueDelta,
                    StatValue = counter.GetValueString(),
                    Statistic = counter.GetDisplayString(),
                    Timestamp = now
                };

                documents.Add(document);
            }

            return Collection.InsertManyAsync(documents, NoValidation);
        }
    }
}
