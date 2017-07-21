namespace Orleans.Providers.MongoDB.Membership.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using global::MongoDB.Bson;
    using global::MongoDB.Driver;

    using Orleans.Providers.MongoDB.Repository;
    using Orleans.Runtime;

    /// <summary>
    /// The mongo membership provider repository.
    /// </summary>
    public class MongoMembershipRepository : DocumentRepository, IMongoMembershipRepository
    {
        /// <summary>
        /// Gets the membership collection name.
        /// </summary>
        public static string MembershipCollectionName
        {
            get
            {
                return "OrleansMembership";
            }
        }

        /// <summary>
        /// The membership version collection name.
        /// </summary>
        private static readonly string MembershipVersionCollectionName = "OrleansMembershipVersion";

        /// <summary>
        /// The membership version key name.
        /// </summary>
        private static readonly string MembershipVersionKeyName = "DeploymentId";
        private static readonly string MembershipKeyName = "DeploymentId";

        public MongoMembershipRepository(string connectionsString, string databaseName)
            : base(connectionsString, databaseName)
        {
        }

        /// <summary>
        /// Init membership version collection.
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task InitMembershipVersionCollectionAsync(string deploymentId)
        {
            if (!await base.CollectionExistsAsync(MembershipCollectionName))
            {
                await base.ReturnOrCreateCollection(MembershipVersionCollectionName).Indexes.CreateOneAsync(Builders<BsonDocument>.IndexKeys.Ascending(m => m[MembershipVersionKeyName]));
                await base.ReturnOrCreateCollection(MembershipCollectionName).Indexes.CreateOneAsync(Builders<BsonDocument>.IndexKeys.Ascending(m => m[MembershipKeyName]));
            }

            BsonDocument membershipVersionDocument =
                await this.FindDocumentAsync(MembershipVersionCollectionName, MembershipVersionKeyName, deploymentId);
            if (membershipVersionDocument == null)
            {
                membershipVersionDocument = new BsonDocument
                                                {
                                                    ["DeploymentId"] = deploymentId,
                                                    ["Timestamp"] = DateTime.UtcNow,
                                                    ["Version"] = 0
                                                };

                await
                    this.SaveDocumentAsync(
                        MembershipVersionCollectionName,
                        MembershipVersionKeyName,
                        deploymentId,
                        membershipVersionDocument);


                //// Todo: This might be overkill
                //await collection.Indexes.CreateOneAsync(Builders<MembershipTable>.IndexKeys.Ascending(m => m.DeploymentId));
            }
        }

        /// <summary>
        /// Insert a membership as well as update the version
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <param name="entry">
        /// The entry.
        /// </param>
        /// <param name="tableVersion">
        /// The table version.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// </exception>
        public async Task<bool> InsertMembershipRow(
            string deploymentId,
            MembershipEntry entry,
            TableVersion tableVersion)
        {
            string address = ReturnAddress(entry.SiloAddress.Endpoint.Address);

            var collection = await ReturnCollection();

            var membershipDocument =
                collection.AsQueryable()
                    .FirstOrDefault(
                        m =>
                        m.DeploymentId == deploymentId && m.Address == address
                        && m.Port == entry.SiloAddress.Endpoint.Port && m.Generation == entry.SiloAddress.Generation);

            if (membershipDocument == null)
            {
                // Todo: Handle as transaction

                if (await UpdateVersion(deploymentId, Convert.ToInt32(tableVersion.VersionEtag), tableVersion.Version))
                {
                    MembershipCollection document = new MembershipCollection
                                                   {
                                                       DeploymentId = deploymentId,
                                                       Address = address,
                                                       Port = entry.SiloAddress.Endpoint.Port,
                                                       Generation = entry.SiloAddress.Generation,
                                                       HostName = entry.HostName,
                                                       Status = (int)entry.Status,
                                                       ProxyPort = entry.ProxyPort,
                                                       StartTime = entry.StartTime,
                                                       IAmAliveTime = entry.IAmAliveTime
                                                   };

                    if (entry.SuspectTimes == null || entry.SuspectTimes.Count == 0)
                    {
                        document.SuspectTimes = string.Empty;
                    }
                    else
                    {
                        document.SuspectTimes = ReturnStringFromSuspectTimes(entry);
                    }

                    if (!collection.AsQueryable().Any(
                        r => r.DeploymentId == document.DeploymentId 
                        && r.Address == document.Address
                        && r.Port == document.Port
                        && r.Generation == document.Generation
                        && r.HostName == document.HostName
                        && r.Status == document.Status
                        && r.ProxyPort == document.ProxyPort
                        && r.StartTime == document.StartTime
                        && r.IAmAliveTime == document.IAmAliveTime))
                    {
                        await collection.InsertOneAsync(document);
                    }
                    else
                    {
                        return false;
                    }

                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Increments a membership version number
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        //private async Task<bool> UpdateVersion(string deploymentId)
        //{
        //    var versionDocument =
        //        await this.FindDocumentAsync(MembershipVersionCollectionName, MembershipVersionKeyName, deploymentId);

        //    if (versionDocument != null)
        //    {
        //        return await UpdateVersion(deploymentId, Convert.ToString(versionDocument["Version"].AsInt32));
        //    }

        //    return false;
        //}

        /// <summary>
        /// Update the membership version.
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <param name="version">
        /// The version.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
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
        /// Return all the deployment members
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        public async Task<MembershipTableData> ReturnAllRows(string deploymentId)
        {
            if (string.IsNullOrEmpty(this.ConnectionString))
            {
                throw new ArgumentException("ConnectionString may not be empty");
            }

            List<MembershipCollection> membershipList =
                Database.GetCollection<MembershipCollection>(MembershipCollectionName)
                    .AsQueryable()
                    .Where(m => m.DeploymentId == deploymentId).ToList();

            return await this.ReturnMembershipTableData(membershipList, deploymentId);
        }

        /// <summary>
        /// Returns a membership for a deployment.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<MembershipTableData> ReturnRow(SiloAddress key, string deploymentId)
        {
            List<MembershipCollection> membershipList =
                Database.GetCollection<MembershipCollection>(MembershipCollectionName)
                    .AsQueryable()
                    .Where(
                        m =>
                        m.DeploymentId == deploymentId && m.Address == ReturnAddress(key.Endpoint.Address)
                        && m.Port == key.Endpoint.Port && m.Generation == key.Generation)
                    .ToList();

            return await this.ReturnMembershipTableData(membershipList, deploymentId);
        }

        /// <summary>
        /// Returns address.
        /// </summary>
        /// <param name="address">
        /// The address.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        static string ReturnAddress(IPAddress address)
        {
            return address.MapToIPv4().ToString();
        }

        /// <summary>
        /// Update i am alive time.
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <param name="siloAddress">
        /// The silo address.
        /// </param>
        /// <param name="iAmAliveTime">
        /// The i am alive time.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task UpdateIAmAliveTimeAsyncTask(
            string deploymentId,
            SiloAddress siloAddress,
            DateTime iAmAliveTime)
        {
            var collection = await ReturnCollection();

            var update = new UpdateDefinitionBuilder<MembershipCollection>().Set(x => x.IAmAliveTime, iAmAliveTime);
            var result =
                await
                collection.UpdateOneAsync(
                    m =>
                    m.DeploymentId == deploymentId && m.Address == ReturnAddress(siloAddress.Endpoint.Address)
                    && m.Port == siloAddress.Endpoint.Port && m.Generation == siloAddress.Generation,
                    update);

            var success = result.ModifiedCount == 1;
        }

        /// <summary>
        /// Parses the MembershipData to a MembershipEntry
        /// </summary>
        /// <param name="membershipData">
        /// The membership data.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
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
                                SiloAddress = ReturnSiloAddress(membershipData),

                                // SiloName = TryGetSiloName(record),
                                HostName = membershipData.HostName,
                                Status = (SiloStatus)membershipData.Status,
                                ProxyPort = membershipData.ProxyPort,
                                StartTime = startTime.Value,
                                IAmAliveTime = membershipData.IAmAliveTime,
                                SiloName = membershipData.HostName
                            };

                string suspectingSilos = membershipData.SuspectTimes;
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

            BsonDocument membershipVersionDocument =
                await
                this.FindDocumentAsync(
                    MembershipVersionCollectionName,
                    MembershipVersionKeyName,
                    membershipData.DeploymentId);

            return Tuple.Create(entry, membershipVersionDocument["Version"].AsInt32.ToString());
        }

        /// <summary>
        /// Returns a silo address.
        /// </summary>
        /// <param name="membershipData">
        /// The membership data.
        /// </param>
        /// <param name="useProxyPort">
        /// The use proxy port.
        /// </param>
        /// <returns>
        /// The <see cref="SiloAddress"/>.
        /// </returns>
        public static SiloAddress ReturnSiloAddress(MembershipCollection membershipData, bool useProxyPort = false)
        {
            // Todo: Move this method to it's own class so it can be shared a bit more elogantly
            int port = membershipData.Port;

            if (useProxyPort)
            {
                port = membershipData.ProxyPort;
            }

            int generation = membershipData.Generation;
            string address = membershipData.Address;
            var siloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Parse(address), port), generation);
            return siloAddress;
        }

        /// <summary>
        /// Returns a MembershipTableData from a list of MembershipTable's
        /// </summary>
        /// <param name="membershipList">
        /// The membership list to be converted.
        /// </param>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private async Task<MembershipTableData> ReturnMembershipTableData(
            List<MembershipCollection> membershipList,
            string deploymentId)
        {
            var membershipVersion = await this.FindDocumentAsync(MembershipVersionCollectionName, MembershipVersionKeyName, deploymentId);
            if (!membershipVersion.Contains("Version"))
            {
                membershipVersion["Version"] = 1;
            }

            var tableVersionEtag = membershipVersion["Version"].AsInt32;

            var membershipEntries = new List<Tuple<MembershipEntry, string>>();

            if (membershipList.Count > 0)
            {
                foreach (var membership in membershipList)
                {
                    membershipEntries.Add(await this.Parse(membership));
                }
            }

            return new MembershipTableData(
                membershipEntries,
                new TableVersion(tableVersionEtag, tableVersionEtag.ToString()));
        }

        /// <summary>
        /// Updates membership row.
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <param name="membershipEntry">
        /// The membership entry.
        /// </param>
        /// <param name="etag">
        /// The etag.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<bool> UpdateMembershipRowAsync(
            string deploymentId,
            MembershipEntry membershipEntry,
            string etag)
        {
            bool verionUpdateResult = await UpdateVersion(deploymentId, Convert.ToInt32(etag), Convert.ToInt32(etag) + 1);
            var collection = await ReturnCollection();

            string suspecttimes = ReturnStringFromSuspectTimes(membershipEntry);

            var update = new UpdateDefinitionBuilder<MembershipCollection>()
                .Set(x => x.Status, (int)membershipEntry.Status)            
                .Set(x => x.SuspectTimes, suspecttimes)
                .Set(x => x.IAmAliveTime, membershipEntry.IAmAliveTime);

            var result = await collection.UpdateOneAsync(
               m => m.DeploymentId == deploymentId && m.Address == ReturnAddress(membershipEntry.SiloAddress.Endpoint.Address)
               && m.Port == membershipEntry.SiloAddress.Endpoint.Port && m.Generation == membershipEntry.SiloAddress.Generation, 
               update);

            bool returnResult = verionUpdateResult || result.ModifiedCount > 0;

            //if (!returnResult)
            //{
            //    // There is an requirement that even though the same data has been updated,
            //    // an update should return true. The Mongo drivers detect that the same data is being passed and therefore
            //    // ModifiedCount is 0. I've Added this extra step to meet the requirement

            //    returnResult = result.MatchedCount > 0;
            //}

            return  returnResult;
        }

        /// <summary>
        /// Returns string from suspect times.
        /// </summary>
        /// <param name="membershipEntry">
        /// The membership entry.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string ReturnStringFromSuspectTimes(MembershipEntry membershipEntry)
        {
            if (membershipEntry.SuspectTimes != null)
            {
                string suspectingSilos = string.Empty;
                foreach (var suspectTime in membershipEntry.SuspectTimes)
                {
                    suspectingSilos += string.Format(
                        "{0}@{1},{2} |",
                        suspectTime.Item1.Endpoint,
                        suspectTime.Item1.Generation,
                        LogFormatter.PrintDate(suspectTime.Item2.ToUniversalTime()));
                }

                return suspectingSilos.TrimEnd('|').TrimEnd(' ');
            }

            return string.Empty;
        }

        /// <summary>
        /// Deletes all memberships for a deployment as well as it version entry.
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task DeleteMembershipTableEntriesAsync(string deploymentId)
        {
            var version = DeleteVersionAsync(deploymentId);
            var membership = DeleteMembershipAsync(deploymentId);

            await Task.WhenAll(version, membership);
        }

        /// <summary>
        /// Deletes all memberships for a deployment.
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private static async Task DeleteMembershipAsync(string deploymentId)
        {
            var collection = await ReturnCollection();
            await collection.DeleteManyAsync(m => m.DeploymentId == deploymentId);
        }

        private async static Task<IMongoCollection<MembershipCollection>> ReturnCollection()
        {
            var collection = Database.GetCollection<MembershipCollection>(MembershipCollectionName);
            return collection;
        }

        /// <summary>
        /// Deletes version for deployment.
        /// </summary>
        /// <param name="deploymentId">
        /// The deployment id.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private async Task DeleteVersionAsync(string deploymentId)
        {
            var versionCollection = this.ReturnOrCreateCollection(MembershipVersionCollectionName);
            var builder = Builders<BsonDocument>.Filter.Eq(MembershipVersionKeyName, deploymentId);
            await versionCollection.DeleteOneAsync(builder);
        }
    }
}