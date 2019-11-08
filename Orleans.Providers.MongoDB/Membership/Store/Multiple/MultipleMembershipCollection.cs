using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Membership.Store.Multiple
{
    public sealed class MultipleMembershipCollection : CollectionBase<MongoMembershipDocument>, IMongoMembershipCollection
    {
        private readonly TableVersionCollection tableVersionCollection;
        private readonly string collectionPrefix;

        public MultipleMembershipCollection(string connectionString, string databaseName, string collectionPrefix, bool createShardKey)
            : base(connectionString, databaseName, createShardKey)
        {
            this.collectionPrefix = collectionPrefix;

            tableVersionCollection = new TableVersionCollection(connectionString, databaseName, collectionPrefix, createShardKey);
        }

        protected override string CollectionName()
        {
            return $"{collectionPrefix}OrleansMembershipV3";
        }

        protected override void SetupCollection(IMongoCollection<MongoMembershipDocument> collection)
        {
            var byDeploymentIdDefinition = Index.Ascending(x => x.DeploymentId);

            collection.Indexes.CreateOne(
                new CreateIndexModel<MongoMembershipDocument>(byDeploymentIdDefinition,
                    new CreateIndexOptions
                    {
                        Name = "ByDeploymentId"
                    }));
        }

        public async Task<bool> UpsertRow(string deploymentId, MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            using (var session = await Client.StartSessionAsync())
            {
                try
                {
                    session.StartTransaction();

                    var hasUpsertedTable = await tableVersionCollection.UpsertAsync(session, tableVersion, deploymentId);

                    if (!hasUpsertedTable)
                    {
                        await session.AbortTransactionAsync();
                        return false;
                    }

                    var hasUpsertedMember = await UpsertRowAsync(session, deploymentId, entry, etag);

                    var isOkay = hasUpsertedMember && hasUpsertedTable;

                    if (isOkay)
                    {
                        await session.CommitTransactionAsync();
                    }
                    else
                    {
                        await session.AbortTransactionAsync();
                    }

                    return isOkay;
                }
                catch (Exception)
                {
                    await session.AbortTransactionAsync();
                    throw;
                }
            }
        }

        private async Task<bool> UpsertRowAsync(IClientSessionHandle session, string deploymentId, MembershipEntry entry, string etag)
        {
            var id = ReturnId(deploymentId, entry.SiloAddress);

            var document = MongoMembershipDocument.Create(entry, deploymentId, id);

            try
            {
                await Collection.ReplaceOneAsync(session, x => x.Id == id && x.Etag == etag, document, Upsert);

                return true;
            }
            catch (MongoException ex)
            {
                if (ex.IsDuplicateKey())
                {
                    return false;
                }
                throw;
            }
        }

        public async Task<IList<Uri>> GetGateways(string deploymentId)
        {
            var entries =
                await Collection.Find(x => x.DeploymentId == deploymentId && x.Status == (int)SiloStatus.Active && x.ProxyPort > 0)
                    .ToListAsync();

            return entries.Select(x => x.ToGatewayUri()).ToList();
        }

        public async Task<MembershipTableData> ReadAll(string deploymentId)
        {
            using (var session = await Client.StartSessionAsync())
            {
                try
                {
                    session.StartTransaction();

                    var tableVersion = tableVersionCollection.GetTableVersionAsync(deploymentId);

                    var entries =
                        Collection.Find(x => x.DeploymentId == deploymentId)
                            .ToListAsync();

                    await Task.WhenAll(tableVersion, entries);

                    await session.CommitTransactionAsync();

                    return ReturnMembershipTableData(entries.Result, tableVersion.Result);
                }
                catch (Exception)
                {
                    await session.AbortTransactionAsync();
                    throw;
                }
            }
        }

        public async Task<MembershipTableData> ReadRow(string deploymentId, SiloAddress address)
        {
            using (var session = await Client.StartSessionAsync())
            {
                try
                {
                    session.StartTransaction();

                    var tableVersion = tableVersionCollection.GetTableVersionAsync(deploymentId);

                    var id = ReturnId(deploymentId, address);

                    var entries =
                        Collection.Find(x => x.Id == id)
                            .ToListAsync();

                    await Task.WhenAll(tableVersion, entries);

                    await session.CommitTransactionAsync();

                    return ReturnMembershipTableData(entries.Result, tableVersion.Result);
                }
                catch (Exception)
                {
                    await session.AbortTransactionAsync();
                    throw;
                }
            }
        }

        public Task UpdateIAmAlive(string deploymentId, SiloAddress address, DateTime iAmAliveTime)
        {
            var id = ReturnId(deploymentId, address);

            return Collection.UpdateOneAsync(x => x.Id == id, Update.Set(x => x.IAmAliveTime, LogFormatter.PrintDate(iAmAliveTime)));
        }

        public Task CleanupDefunctSiloEntries(string deploymentId, DateTimeOffset beforeDate)
        {
            return Collection.DeleteManyAsync(x => x.DeploymentId == deploymentId && x.Status == (int)SiloStatus.Dead && x.Timestamp < beforeDate);
        }

        public Task DeleteMembershipTableEntries(string deploymentId)
        {
            return Task.WhenAll(
                Collection.DeleteManyAsync(x => x.DeploymentId == deploymentId),
                tableVersionCollection.DeleteAsync(deploymentId)
            );
        }

        private static MembershipTableData ReturnMembershipTableData(IEnumerable<MongoMembershipDocument> membershipList, TableVersion tableVersion)
        {
            return new MembershipTableData(membershipList.Select(x => Tuple.Create(x.ToEntry(), x.Etag)).ToList(), tableVersion);
        }

        private static string ReturnAddress(IPAddress address)
        {
            return address.MapToIPv4().ToString();
        }

        private static string ReturnId(string deploymentId, SiloAddress address)
        {
            return $"{deploymentId}@{ReturnAddress(address.Endpoint.Address)}:{address.Endpoint.Port}/{address.Generation}";
        }
    }
}
