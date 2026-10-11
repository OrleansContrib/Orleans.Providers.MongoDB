using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Membership.Store.Multiple
{
    public sealed class MultipleMembershipCollection : CollectionBase<MongoMembershipDocument>, IMongoMembershipCollection
    {
        private const int MaxConsistentReadAttempts = 10;
        private readonly TableVersionCollection tableVersionCollection;
        private readonly string collectionPrefix;

        public MultipleMembershipCollection(
            IMongoClient mongoClient,
            string databaseName,
            string collectionPrefix,
            Action<MongoCollectionSettings> collectionConfigurator,
            bool createShardKey)
            : base(mongoClient, databaseName, collectionConfigurator, createShardKey)
        {
            this.collectionPrefix = collectionPrefix;

            tableVersionCollection = new TableVersionCollection(mongoClient, databaseName, collectionPrefix, collectionConfigurator, createShardKey);
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

        public async Task<bool> UpsertRow(string deploymentId, MembershipEntry entry, string etag, TableVersion tableVersion, CancellationToken cancellationToken = default)
        {
            using var session = await Client.StartSessionAsync(cancellationToken: cancellationToken);
            return await session.WithTransactionAsync(async (sessionHandle, ct) =>
            {
                var hasUpsertedTable = await tableVersionCollection.UpsertAsync(sessionHandle, tableVersion, deploymentId, ct);

                if (!hasUpsertedTable)
                {
                    await sessionHandle.AbortTransactionAsync(ct);
                    return false;
                }

                var hasUpsertedMember = await UpsertRowAsync(sessionHandle, deploymentId, entry, etag, ct);

                var isOkay = hasUpsertedMember && hasUpsertedTable;

                if (!isOkay)
                {
                    await sessionHandle.AbortTransactionAsync(ct);
                }

                return isOkay;
            }, cancellationToken: cancellationToken);
        }

        private async Task<bool> UpsertRowAsync(IClientSessionHandle session, string deploymentId, MembershipEntry entry, string etag, CancellationToken cancellationToken)
        {
            var id = ReturnId(deploymentId, entry.SiloAddress);

            var document = MongoMembershipDocument.Create(entry, deploymentId, id);

            try
            {
                if (etag == null)
                {
                    await Collection.InsertOneAsync(session, document, cancellationToken: cancellationToken);

                    return true;
                }

                var result = await Collection.ReplaceOneAsync(session, x => x.Id == id && x.Etag == etag, document, cancellationToken: cancellationToken);

                return result.MatchedCount == 1;
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

        public async Task<IList<Uri>> GetGateways(string deploymentId, CancellationToken cancellationToken = default)
        {
            var entries =
                await Collection.Find(x => x.DeploymentId == deploymentId && x.Status == (int)SiloStatus.Active && x.ProxyPort > 0)
                    .ToListAsync(cancellationToken);

            return entries.Select(x => x.ToGatewayUri()).ToList();
        }

        public Task<MembershipTableData> ReadAll(string deploymentId, CancellationToken cancellationToken = default)
        {
            return ReadConsistentAsync(deploymentId, session =>
                Collection.Find(session, x => x.DeploymentId == deploymentId).ToListAsync(cancellationToken), cancellationToken);
        }

        public Task<MembershipTableData> ReadRow(string deploymentId, SiloAddress address, CancellationToken cancellationToken = default)
        {
            var id = ReturnId(deploymentId, address);

            return ReadConsistentAsync(deploymentId, session =>
                Collection.Find(session, x => x.Id == id).ToListAsync(cancellationToken), cancellationToken);
        }

        // The rows and the table version are stored in different documents, but every write updates both in one transaction
        // and assigns a new version etag. The rows therefore belong to the table version if it did not change while they were read.
        private async Task<MembershipTableData> ReadConsistentAsync(
            string deploymentId,
            Func<IClientSessionHandle, Task<List<MongoMembershipDocument>>> readEntries,
            CancellationToken cancellationToken)
        {
            using var session = await Client.StartSessionAsync(cancellationToken: cancellationToken);

            for (var attempt = 1; ; attempt++)
            {
                var tableVersion = await tableVersionCollection.GetTableVersionAsync(session, deploymentId, cancellationToken);

                var entries = await readEntries(session);

                var tableVersionAfterRead = await tableVersionCollection.GetTableVersionAsync(session, deploymentId, cancellationToken);

                if (tableVersion.Equals(tableVersionAfterRead))
                {
                    return ReturnMembershipTableData(entries, tableVersion);
                }

                if (attempt == MaxConsistentReadAttempts)
                {
                    throw new InvalidOperationException(
                        $"Membership table for deployment {deploymentId} changed during {MaxConsistentReadAttempts} consecutive reads.");
                }
            }
        }

        public Task UpdateIAmAlive(string deploymentId, SiloAddress address, DateTime iAmAliveTime, CancellationToken cancellationToken = default)
        {
            var id = ReturnId(deploymentId, address);

            return Collection.UpdateOneAsync(x => x.Id == id, Update.Set(x => x.IAmAliveTime, LogFormatter.PrintDate(iAmAliveTime)), cancellationToken: cancellationToken);
        }

        public Task CleanupDefunctSiloEntries(string deploymentId, DateTimeOffset beforeDate, CancellationToken cancellationToken = default)
        {
            return MembershipCleanup.DeleteDefunctSiloEntriesAsync(Collection, deploymentId, beforeDate, cancellationToken);
        }

        public async Task DeleteMembershipTableEntries(string deploymentId, CancellationToken cancellationToken = default)
        {
            using var session = await Client.StartSessionAsync(cancellationToken: cancellationToken);

            await session.WithTransactionAsync(async (sessionHandle, ct) =>
            {
                await Collection.DeleteManyAsync(sessionHandle, x => x.DeploymentId == deploymentId, cancellationToken: ct);
                await tableVersionCollection.DeleteAsync(sessionHandle, deploymentId, ct);
                return true;
            }, cancellationToken: cancellationToken);
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
