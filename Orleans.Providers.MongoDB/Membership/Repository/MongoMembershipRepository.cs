using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Orleans.Providers.MongoDB.Repository;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Membership.Repository
{
    /// <summary>
    ///     The mongo membership provider repository.
    /// </summary>
    public class MongoMembershipRepository : DocumentRepository, IMongoMembershipRepository
    {
        /// <summary>
        ///     The membership version collection name.
        /// </summary>
        private const string MembershipVersionCollectionName = "OrleansMembershipVersion";

        /// <summary>
        ///     The membership version key name.
        /// </summary>
        private const string MembershipVersionKeyName = "DeploymentId";

        private const string MembershipKeyName = "DeploymentId";

        public MongoMembershipRepository(string connectionsString, string databaseName)
            : base(connectionsString, databaseName)
        {
        }

        /// <summary>
        ///     Gets the membership collection name.
        /// </summary>
        public static string MembershipCollectionName => "OrleansMembership";

        /// <summary>
        ///     Init membership version collection.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        public async Task InitMembershipVersionCollectionAsync(string deploymentId)
        {
            if (!await CollectionExistsAsync(MembershipCollectionName))
            {
                await ReturnOrCreateCollection(MembershipVersionCollectionName).Indexes
                    .CreateOneAsync(Builders<BsonDocument>.IndexKeys.Ascending(m => m[MembershipVersionKeyName]));
                await ReturnOrCreateCollection(MembershipCollectionName).Indexes
                    .CreateOneAsync(Builders<BsonDocument>.IndexKeys.Ascending(m => m[MembershipKeyName]));
            }

            var membershipVersionDocument =
                await FindDocumentAsync(MembershipVersionCollectionName, MembershipVersionKeyName, deploymentId);
            if (membershipVersionDocument == null)
            {
                membershipVersionDocument = new BsonDocument
                {
                    ["DeploymentId"] = deploymentId,
                    ["Timestamp"] = DateTime.UtcNow,
                    ["Version"] = 0
                };

                await
                    SaveDocumentAsync(
                        MembershipVersionCollectionName,
                        MembershipVersionKeyName,
                        deploymentId,
                        membershipVersionDocument);


                //// Todo: This might be overkill
                //await collection.Indexes.CreateOneAsync(Builders<MembershipTable>.IndexKeys.Ascending(m => m.DeploymentId));
            }
        }

        /// <summary>
        ///     Insert a membership as well as update the version
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <param name="entry">
        ///     The entry.
        /// </param>
        /// <param name="tableVersion">
        ///     The table version.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="Exception">
        /// </exception>
        public async Task<bool> InsertMembershipRow(
            string deploymentId,
            MembershipEntry entry,
            TableVersion tableVersion)
        {
            var address = ReturnAddress(entry.SiloAddress.Endpoint.Address);

            var collection = ReturnMembershipCollection();

            var membershipDocumentCursor = await collection.FindAsync(m =>
                m.DeploymentId == deploymentId && m.Address == address
                && m.Port == entry.SiloAddress.Endpoint.Port && m.Generation == entry.SiloAddress.Generation);

            var membershipDocument = await membershipDocumentCursor.ToListAsync();

            if (membershipDocument.Count != 0) return false;
            if (!await UpdateVersion(deploymentId, Convert.ToInt32(tableVersion.VersionEtag),
                tableVersion.Version)) return false;

            var document = new MembershipCollection
            {
                DeploymentId = deploymentId,
                Address = address,
                Port = entry.SiloAddress.Endpoint.Port,
                Generation = entry.SiloAddress.Generation,
                HostName = entry.HostName,
                Status = (int) entry.Status,
                ProxyPort = entry.ProxyPort,
                StartTime = entry.StartTime,
                IAmAliveTime = entry.IAmAliveTime
            };

            if (entry.SuspectTimes == null || entry.SuspectTimes.Count == 0)
                document.SuspectTimes = string.Empty;
            else
                document.SuspectTimes = MembershipHelper.ReturnStringFromSuspectTimes(entry);

            var cursorfoundMemberships = await collection.FindAsync(r => r.DeploymentId == document.DeploymentId
                                                                         && r.Address == document.Address
                                                                         && r.Port == document.Port
                                                                         && r.Generation == document.Generation
                                                                         && r.HostName == document.HostName
                                                                         && r.Status == document.Status
                                                                         && r.ProxyPort == document.ProxyPort
                                                                         && r.StartTime == document.StartTime
                                                                         && r.IAmAliveTime == document.IAmAliveTime);

            var foundMemberships = await cursorfoundMemberships.ToListAsync();

            if (foundMemberships.Count == 0)
                await collection.InsertOneAsync(document);
            else
                return false;

            return true;
        }

        /// <summary>
        ///     Return all the deployment members
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        public async Task<MembershipTableData> ReturnAllRows(string deploymentId)
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new ArgumentException("ConnectionString may not be empty");

            var membershipListCursor = await Database.GetCollection<MembershipCollection>(MembershipCollectionName)
                .FindAsync(m => m.DeploymentId == deploymentId);

            var membershipList = await membershipListCursor.ToListAsync();

            return await ReturnMembershipTableData(membershipList, deploymentId);
        }

        /// <summary>
        ///     Returns a membership for a deployment.
        /// </summary>
        /// <param name="key">
        ///     The key.
        /// </param>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        public async Task<MembershipTableData> ReturnRow(SiloAddress key, string deploymentId)
        {
            var membershipListCursor = await Database.GetCollection<MembershipCollection>(MembershipCollectionName)
                .FindAsync(m =>
                    m.DeploymentId == deploymentId && m.Address == ReturnAddress(key.Endpoint.Address)
                    && m.Port == key.Endpoint.Port && m.Generation == key.Generation);

            var membershipList = await membershipListCursor.ToListAsync();

            return await ReturnMembershipTableData(membershipList, deploymentId);
        }


        /// <summary>
        ///     Update i am alive time.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <param name="siloAddress">
        ///     The silo address.
        /// </param>
        /// <param name="iAmAliveTime">
        ///     The i am alive time.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        public async Task UpdateIAmAliveTimeAsyncTask(
            string deploymentId,
            SiloAddress siloAddress,
            DateTime iAmAliveTime)
        {
            var collection = ReturnMembershipCollection();

            var update = new UpdateDefinitionBuilder<MembershipCollection>().Set(x => x.IAmAliveTime, iAmAliveTime);
            await collection.UpdateOneAsync(
                m =>
                    m.DeploymentId == deploymentId && m.Address == ReturnAddress(siloAddress.Endpoint.Address)
                    && m.Port == siloAddress.Endpoint.Port && m.Generation == siloAddress.Generation,
                update);
        }

        /// <summary>
        ///     Updates membership row.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <param name="membershipEntry">
        ///     The membership entry.
        /// </param>
        /// <param name="etag">
        ///     The etag.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        public async Task<bool> UpdateMembershipRowAsync(
            string deploymentId,
            MembershipEntry membershipEntry,
            string etag)
        {
            var verionUpdateResult =
                await UpdateVersion(deploymentId, Convert.ToInt32(etag), Convert.ToInt32(etag) + 1);
            var collection = ReturnMembershipCollection();

            var suspecttimes = MembershipHelper.ReturnStringFromSuspectTimes(membershipEntry);

            var update = new UpdateDefinitionBuilder<MembershipCollection>()
                .Set(x => x.Status, (int) membershipEntry.Status)
                .Set(x => x.SuspectTimes, suspecttimes)
                .Set(x => x.IAmAliveTime, membershipEntry.IAmAliveTime);

            var result = await collection.UpdateOneAsync(
                m => m.DeploymentId == deploymentId &&
                     m.Address == ReturnAddress(membershipEntry.SiloAddress.Endpoint.Address)
                     && m.Port == membershipEntry.SiloAddress.Endpoint.Port &&
                     m.Generation == membershipEntry.SiloAddress.Generation,
                update);

            var returnResult = verionUpdateResult || result.ModifiedCount > 0;

            //if (!returnResult)
            //{
            //    // There is an requirement that even though the same data has been updated,
            //    // an update should return true. The Mongo drivers detect that the same data is being passed and therefore
            //    // ModifiedCount is 0. I've Added this extra step to meet the requirement

            //    returnResult = result.MatchedCount > 0;
            //}

            return returnResult;
        }

        /// <summary>
        ///     Deletes all memberships for a deployment as well as it version entry.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        public async Task DeleteMembershipTableEntriesAsync(string deploymentId)
        {
            var version = DeleteVersionAsync(deploymentId);
            var membership = DeleteMembershipAsync(deploymentId);

            await Task.WhenAll(version, membership);
        }

        /// <summary>
        ///     Update the membership version.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <param name="version">
        ///     The version.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        private static async Task<bool> UpdateVersion(string deploymentId, int perviousVersion, int newVersion)
        {
            var collection = Database.GetCollection<BsonDocument>(MembershipVersionCollectionName);

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("DeploymentId", deploymentId) & builder.Eq("Version", perviousVersion);

            var result =
                await
                    collection.UpdateOneAsync(
                        filter,
                        Builders<BsonDocument>.Update.Set("Version", newVersion)
                            .Set("Timestamp", DateTime.Now.ToUniversalTime()));

            return result.ModifiedCount > 0;
        }

        /// <summary>
        ///     Returns address.
        /// </summary>
        /// <param name="address">
        ///     The address.
        /// </param>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        private static string ReturnAddress(IPAddress address)
        {
            return address.MapToIPv4().ToString();
        }

        /// <summary>
        ///     Parses the MembershipData to a MembershipEntry
        /// </summary>
        /// <param name="membershipData">
        ///     The membership data.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        internal async Task<Tuple<MembershipEntry, string>> Parse(MembershipCollection membershipData)
        {
            // TODO: This is a bit of hack way to check in the current version if there's membership data or not, but if there's a start time, there's member.            
            DateTime? startTime = membershipData.StartTime;
            MembershipEntry entry = null;
            if (startTime.HasValue)
            {
                entry = new MembershipEntry
                {
                    SiloAddress = MembershipHelper.ReturnSiloAddress(membershipData),

                    // SiloName = TryGetSiloName(record),
                    HostName = membershipData.HostName,
                    Status = (SiloStatus) membershipData.Status,
                    ProxyPort = membershipData.ProxyPort,
                    StartTime = startTime.Value,
                    IAmAliveTime = membershipData.IAmAliveTime,
                    SiloName = membershipData.HostName
                };

                var suspectingSilos = membershipData.SuspectTimes;
                if (!string.IsNullOrWhiteSpace(suspectingSilos))
                {
                    entry.SuspectTimes = new List<Tuple<SiloAddress, DateTime>>();
                    entry.SuspectTimes.AddRange(
                        suspectingSilos.Split('|').Select(
                            s =>
                            {
                                var split = s.Split(',');
                                return new Tuple<SiloAddress, DateTime>(
                                    SiloAddress.FromParsableString(split[0].Trim()),
                                    LogFormatter.ParseDate(split[1].Trim()));
                            }));
                }
            }

            var membershipVersionDocument =
                await
                    FindDocumentAsync(
                        MembershipVersionCollectionName,
                        MembershipVersionKeyName,
                        membershipData.DeploymentId);

            return Tuple.Create(entry, membershipVersionDocument["Version"].AsInt32.ToString());
        }

        /// <summary>
        ///     Deletes all memberships for a deployment.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        private static async Task DeleteMembershipAsync(string deploymentId)
        {
            var collection = ReturnMembershipCollection();
            await collection.DeleteManyAsync(m => m.DeploymentId == deploymentId);
        }

        private static IMongoCollection<MembershipCollection> ReturnMembershipCollection()
        {
            var collection = Database.GetCollection<MembershipCollection>(MembershipCollectionName);
            return collection;
        }

        /// <summary>
        ///     Deletes version for deployment.
        /// </summary>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        private async Task DeleteVersionAsync(string deploymentId)
        {
            var versionCollection = ReturnOrCreateCollection(MembershipVersionCollectionName);
            var builder = Builders<BsonDocument>.Filter.Eq(MembershipVersionKeyName, deploymentId);
            await versionCollection.DeleteOneAsync(builder);
        }

        /// <summary>
        ///     Returns a MembershipTableData from a list of MembershipTable's
        /// </summary>
        /// <param name="membershipList">
        ///     The membership list to be converted.
        /// </param>
        /// <param name="deploymentId">
        ///     The deployment id.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        private async Task<MembershipTableData> ReturnMembershipTableData(
            List<MembershipCollection> membershipList,
            string deploymentId)
        {
            var membershipVersion =
                await FindDocumentAsync(MembershipVersionCollectionName, MembershipVersionKeyName, deploymentId);
            if (!membershipVersion.Contains("Version"))
                membershipVersion["Version"] = 1;

            var tableVersionEtag = membershipVersion["Version"].AsInt32;

            var membershipEntries = new List<Tuple<MembershipEntry, string>>();

            if (membershipList.Count > 0)
                foreach (var membership in membershipList)
                    membershipEntries.Add(await Parse(membership));

            return new MembershipTableData(
                membershipEntries,
                new TableVersion(tableVersionEtag, tableVersionEtag.ToString()));
        }
    }
}