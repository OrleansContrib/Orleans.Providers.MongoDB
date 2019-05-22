using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Utils;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Membership.Store
{
    public sealed class MongoMembershipCollection : CollectionBase<MongoMembershipDocument>
    {
        // MongoDB does not support the extended Membership Protocol and will always return the same table version information
        private static readonly TableVersion tableVersion = new TableVersion(0, "0");
        private static readonly UpdateOptions Upsert = new UpdateOptions { IsUpsert = true };
        private readonly string collectionPrefix;

        public MongoMembershipCollection(string connectionString, string databaseName, string collectionPrefix)
            : base(connectionString, databaseName)
        {
            this.collectionPrefix = collectionPrefix;
        }

        protected override string CollectionName()
        {
            return collectionPrefix + "OrleansMembershipV2";
        }

        protected override void SetupCollection(IMongoCollection<MongoMembershipDocument> collection)
        {
            collection.Indexes.CreateOne(new CreateIndexModel<MongoMembershipDocument>(Index.Ascending(x => x.DeploymentId)));
        }

        public async Task<bool> UpsertRow(string deploymentId, MembershipEntry entry, string etag)
        {
            var id = ReturnId(deploymentId, entry.SiloAddress);

            var document = MongoMembershipDocument.Create(entry, deploymentId, Guid.NewGuid().ToString(), id);

            try
            {
                await Collection.ReplaceOneAsync(x => x.Id == id && x.Etag == etag, document, Upsert);

                return true;
            }
            catch (MongoWriteException ex)
            {
                if (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
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

            return entries.Select(ReturnGatewayUri).ToList();
        }

        public async Task<MembershipTableData> ReadAll(string deploymentId)
        {
            var entries =
                await Collection.Find(x => x.DeploymentId == deploymentId)
                    .ToListAsync();

            return ReturnMembershipTableData(entries);
        }
        
        public async Task<MembershipTableData> ReadRow(string deploymentId, SiloAddress address)
        {
            var id = ReturnId(deploymentId, address);

            var entries =
                await Collection.Find(x => x.Id == id)
                    .ToListAsync();

            return ReturnMembershipTableData(entries);
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
            return Collection.DeleteManyAsync(x => x.DeploymentId == deploymentId);
        }

        private static MembershipTableData ReturnMembershipTableData(IEnumerable<MongoMembershipDocument> membershipList)
        {
            return new MembershipTableData(membershipList.Select(x => Tuple.Create(x.ToEntry(), x.Etag)).ToList(), tableVersion);
        }

        private static string ReturnAddress(IPAddress address)
        {
            return address.MapToIPv4().ToString();
        }

        private static Uri ReturnGatewayUri(MongoMembershipDocument record)
        {
            var siloAddress = SiloAddress.FromParsableString(record.SiloAddress);

            return SiloAddress.New(new IPEndPoint(siloAddress.Endpoint.Address, record.ProxyPort), siloAddress.Generation).ToGatewayUri();
        }

        private static string ReturnId(string deploymentId, SiloAddress address)
        {
            return $"{deploymentId}@{ReturnAddress(address.Endpoint.Address)}:{address.Endpoint.Port}/{address.Generation}";
        }
    }
}
