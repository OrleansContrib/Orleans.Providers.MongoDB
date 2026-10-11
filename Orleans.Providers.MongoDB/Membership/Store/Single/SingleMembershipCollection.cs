using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Membership.Store.Single
{
    public sealed class SingleMembershipCollection : CollectionBase<DeploymentDocument>, IMongoMembershipCollection
    {
        private static readonly TableVersion NotFound = new TableVersion(0, "0");
        private readonly string collectionPrefix;

        public SingleMembershipCollection(IMongoClient mongoClient, string databaseName, string collectionPrefix, bool createShardKey,
            Action<MongoCollectionSettings> collectionConfigurator)
            : base(mongoClient, databaseName, collectionConfigurator, createShardKey)
        {
            this.collectionPrefix = collectionPrefix;
        }

        protected override string CollectionName()
        {
            return $"{collectionPrefix}OrleansMembershipSingle";
        }

        public async Task CleanupDefunctSiloEntries(string deploymentId, DateTimeOffset beforeDate, CancellationToken cancellationToken = default)
        {
            var deployment = await Collection.Find(x => x.DeploymentId == deploymentId).FirstOrDefaultAsync(cancellationToken);

            if (deployment == null)
            {
                return;
            }

            var beforeUtc = beforeDate.UtcDateTime;

            var removals = new List<WriteModel<DeploymentDocument>>();

            foreach (var kvp in deployment.Members)
            {
                if (kvp.Value.IsDefunct(beforeUtc))
                {
                    // Only remove the revisions that were evaluated, so that concurrent updates are never lost. Each member has
                    // its own precondition, so one changed member does not prevent the removal of the others. Heartbeats change
                    // IAmAliveTime without assigning a new etag, so both are part of the precondition.
                    removals.Add(new UpdateOneModel<DeploymentDocument>(
                        Filter.And(
                            Filter.Eq(x => x.DeploymentId, deploymentId),
                            Filter.Eq($"Members.{kvp.Key}.Etag", kvp.Value.Etag),
                            Filter.Eq($"Members.{kvp.Key}.IAmAliveTime", kvp.Value.IAmAliveTime)),
                        Update.Unset($"Members.{kvp.Key}")));
                }
            }

            if (removals.Count > 0)
            {
                await Collection.BulkWriteAsync(removals, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
            }
        }

        public Task DeleteMembershipTableEntries(string deploymentId, CancellationToken cancellationToken = default)
        {
            return Collection.DeleteOneAsync(x => x.DeploymentId == deploymentId, cancellationToken);
        }

        public async Task<IList<Uri>> GetGateways(string deploymentId, CancellationToken cancellationToken = default)
        {
            var deployment = await Collection.Find(x => x.DeploymentId == deploymentId).FirstOrDefaultAsync(cancellationToken);

            if (deployment == null)
            {
                return new List<Uri>();
            }

            return deployment.Members.Values.Where(x => x.Status == (int)SiloStatus.Active && x.ProxyPort > 0).Select(x => x.ToGatewayUri()).ToList();
        }

        public async Task<MembershipTableData> ReadAll(string deploymentId, CancellationToken cancellationToken = default)
        {
            var deployment = await Collection.Find(x => x.DeploymentId == deploymentId).FirstOrDefaultAsync(cancellationToken);

            if (deployment == null)
            {
                return new MembershipTableData(NotFound);
            }

            return deployment.ToTable();
        }

        public async Task<MembershipTableData> ReadRow(string deploymentId, SiloAddress address, CancellationToken cancellationToken = default)
        {
            var deployment = await Collection.Find(x => x.DeploymentId == deploymentId).FirstOrDefaultAsync(cancellationToken);

            if (deployment == null)
            {
                return new MembershipTableData(NotFound);
            }

            return deployment.ToTable(BuildKey(address));
        }

        public async Task UpdateIAmAlive(string deploymentId, SiloAddress address, DateTime iAmAliveTime, CancellationToken cancellationToken = default)
        {
            var filter = Builders<DeploymentDocument>.Filter.And(
                Builders<DeploymentDocument>.Filter.Eq(x => x.DeploymentId, deploymentId),
                Builders<DeploymentDocument>.Filter.Exists($"Members.{BuildKey(address)}", true));

            await Collection.UpdateOneAsync(filter, Update
                    .Set($"Members.{BuildKey(address)}.IAmAliveTime", LogFormatter.PrintDate(iAmAliveTime)), cancellationToken: cancellationToken);
        }

        public async Task<bool> UpsertRow(string deploymentId, MembershipEntry entry, string etag, TableVersion tableVersion, CancellationToken cancellationToken = default)
        {
            try
            {
                var subDocument = MembershipBase.Create<DeploymentMembership>(entry);

                var memberKey = $"Members.{BuildKey(entry.SiloAddress)}";

                var etagCheck = 
                    etag == null ?
                        Filter.Not(Filter.Exists(memberKey)) :
                        Filter.Eq($"{memberKey}.Etag", etag);

                var result = await Collection.UpdateOneAsync(
                    Filter.And(
                        Filter.Eq(x => x.DeploymentId, deploymentId),
                        Filter.Eq(x => x.VersionEtag, tableVersion.VersionEtag),
                        etagCheck),
                    Update
                        .Set(memberKey, subDocument)
                        .Set(x => x.Version, tableVersion.Version)
                        .Set(x => x.VersionEtag, EtagHelper.CreateNew()),
                    Upsert,
                    cancellationToken);

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

        private static string BuildKey(SiloAddress address)
        {
            return address.ToParsableString().Replace('.', '_');
        }
    }
}
