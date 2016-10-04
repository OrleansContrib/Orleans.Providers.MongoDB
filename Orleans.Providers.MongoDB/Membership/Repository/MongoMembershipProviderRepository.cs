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
    public class MongoMembershipProviderRepository : DocumentRepository, IMongoMembershipProviderRepository
    {
        // Todo: Not sure why I can't see (Orleans.Runtime.LogFormatter.ParseDate
        private const string DATE_FORMAT = "yyyy-MM-dd " + TIME_FORMAT;

                             // Example: 2010-09-02 09:50:43.341 GMT - Variant of UniversalSorta­bleDateTimePat­tern
        private const string TIME_FORMAT = "HH:mm:ss.fff 'GMT'"; // Example: 09:50:43.341 GMT

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

        public MongoMembershipProviderRepository(string connectionsString, string databaseName)
            : base(connectionsString, databaseName)
        {
        }

        /// <summary>
        /// The parse date.
        /// </summary>
        /// <param name="dateStr">
        /// The date str.
        /// </param>
        /// <returns>
        /// The <see cref="DateTime"/>.
        /// </returns>
        public static DateTime ParseDate(string dateStr)
        {
            return DateTime.ParseExact(dateStr, DATE_FORMAT, CultureInfo.InvariantCulture);
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

            var collection = Database.GetCollection<MembershipTable>(MembershipCollectionName);

            var membershipDocument =
                collection.AsQueryable()
                    .FirstOrDefault(
                        m =>
                        m.DeploymentId == deploymentId && m.Address == address
                        && m.Port == entry.SiloAddress.Endpoint.Port && m.Generation == entry.SiloAddress.Generation);

            if (membershipDocument == null)
            {
                // Todo: Handle as transaction
                MembershipTable document = new MembershipTable
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

                if (entry.SuspectTimes.Count == 0)
                {
                    document.SuspectTimes = string.Empty;
                }
                else
                {
                    throw new Exception();
                }

                await collection.InsertOneAsync(document);
                await this.UpdateVersion(deploymentId);
            }

            return true;
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
        private async Task UpdateVersion(string deploymentId)
        {
            var versionDocument =
                await this.FindDocumentAsync(MembershipVersionCollectionName, MembershipVersionKeyName, deploymentId);

            if (versionDocument != null)
            {
                await UpdateVersion(deploymentId, Convert.ToString(versionDocument["Version"].AsInt32));
            }
        }

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
        private static async Task UpdateVersion(string deploymentId, string version)
        {
            var collection = Database.GetCollection<BsonDocument>(MembershipVersionCollectionName);

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("DeploymentId", deploymentId) & builder.Eq("Version", Convert.ToInt32(version));

            var result =
                await
                collection.UpdateOneAsync(
                    filter,
                    Builders<BsonDocument>.Update.Set("Version", Convert.ToInt32(version) + 1)
                    .Set("Timestamp", DateTime.Now.ToUniversalTime()));
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

            List<MembershipTable> membershipList =
                Database.GetCollection<MembershipTable>(MembershipCollectionName)
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
            List<MembershipTable> membershipList =
                Database.GetCollection<MembershipTable>(MembershipCollectionName)
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
            var collection = Database.GetCollection<MembershipTable>(MembershipCollectionName);

            var update = new UpdateDefinitionBuilder<MembershipTable>().Set(x => x.IAmAliveTime, iAmAliveTime);
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
        internal async Task<Tuple<MembershipEntry, string>> Parse(MembershipTable membershipData)
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
                                InstanceName = membershipData.HostName
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
                                        SiloAddress.FromParsableString(split[0]),
                                        ParseDate(split[1]));
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
        public static SiloAddress ReturnSiloAddress(MembershipTable membershipData, bool useProxyPort = false)
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
            List<MembershipTable> membershipList,
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
            await UpdateVersion(deploymentId, etag);
            var collection = Database.GetCollection<MembershipTable>(MembershipCollectionName);

            string suspecttimes = ReturnStringFromSuspectTimes(membershipEntry);

            var update = new UpdateDefinitionBuilder<MembershipTable>()
                .Set(x => x.Status, (int)membershipEntry.Status)            
                .Set(x => x.SuspectTimes, suspecttimes)
                .Set(x => x.IAmAliveTime, membershipEntry.IAmAliveTime);

            var result = await collection.UpdateOneAsync(
               m => m.DeploymentId == deploymentId && m.Address == ReturnAddress(membershipEntry.SiloAddress.Endpoint.Address)
               && m.Port == membershipEntry.SiloAddress.Endpoint.Port && m.Generation == membershipEntry.SiloAddress.Generation, 
               update);

            var success = result.ModifiedCount == 1;

            return true;
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
                    suspectingSilos = string.Format(
                        "{0}@{1},{2} |",
                        suspectTime.Item1.Endpoint,
                        suspectTime.Item1.Generation,
                        suspectTime.Item2.ToUniversalTime().ToString(DATE_FORMAT));
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
            var collection = Database.GetCollection<MembershipTable>(MembershipCollectionName);
            await collection.DeleteOneAsync(m => m.DeploymentId == deploymentId);
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