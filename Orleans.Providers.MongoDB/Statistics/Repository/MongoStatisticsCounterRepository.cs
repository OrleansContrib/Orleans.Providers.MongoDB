using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Repository;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Statistics.Repository
{
    public class MongoStatisticsCounterRepository : DocumentRepository2<OrleansStatisticsTable>
    {
        private static readonly UpdateOptions UpsertNoValidation = new UpdateOptions { BypassDocumentValidation = true, IsUpsert = true };

        public MongoStatisticsCounterRepository(string connectionString, string databaseName)
            : base(connectionString, databaseName)
        {
        }

        protected override string CollectionName()
        {
            return "OrleansStatisticsTable";
        }

        public virtual Task InsertStatisticsCountersAsync(OrleansStatisticsTable statisticsTable, List<ICounter> counterBatch)
        {
            var documents = new List<OrleansStatisticsTable>();

            OrleansStatisticsTable newStatisticTable = null;

            foreach (var counter in counterBatch)
            {
                newStatisticTable = new OrleansStatisticsTable
                {
                    DeploymentId = statisticsTable.DeploymentId,
                    HostName = statisticsTable.HostName,
                    Name = statisticsTable.Name,
                    Identity = statisticsTable.Id
                };

                newStatisticTable.IsValueDelta = counter.IsValueDelta;
                newStatisticTable.StatValue = counter.GetValueString();
                newStatisticTable.Statistic = counter.GetDisplayString();

                documents.Add(newStatisticTable);
            }
            
            return Collection.InsertManyAsync(documents, new InsertManyOptions { BypassDocumentValidation = true });
        }
    }
}
