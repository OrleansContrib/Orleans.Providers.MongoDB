using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Repository;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Statistics.Store
{
    public class MongoStatisticsCounterCollection : CollectionBase<MongoStatisticsCounterDocument>
    {
        private static readonly InsertManyOptions NoValidation = new InsertManyOptions { BypassDocumentValidation = true };

        public MongoStatisticsCounterCollection(string connectionString, string databaseName)
            : base(connectionString, databaseName)
        {
        }

        protected override string CollectionName()
        {
            return "OrleansStatisticsTable";
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
