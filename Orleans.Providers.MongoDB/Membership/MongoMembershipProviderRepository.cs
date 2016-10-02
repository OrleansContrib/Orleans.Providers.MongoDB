using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Membership
{
    using System.Globalization;
    using System.Net;
    using System.Reflection;
    using System.Threading;

    using global::MongoDB.Bson;
    using global::MongoDB.Driver;

    using Orleans.Providers.MongoDB.Repository;
    using Orleans.Runtime;

    public class MongoMembershipProviderRepository : DocumentRepository, IMongoMembershipProviderRepository
    {
        //Todo: Not sure why I can't see (Orleans.Runtime.LogFormatter
        private const string TIME_FORMAT = "HH:mm:ss.fff 'GMT'"; // Example: 09:50:43.341 GMT
        private const string DATE_FORMAT = "yyyy-MM-dd " + TIME_FORMAT; // Example: 2010-09-02 09:50:43.341 GMT - Variant of UniversalSorta­bleDateTimePat­tern

        public static DateTime ParseDate(string dateStr)
        {
            return DateTime.ParseExact(dateStr, DATE_FORMAT, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The membership version collection name.
        /// </summary>
        private static readonly string MembershipVersionCollectionName = "OrleansMembershipVersion";
        private static readonly string MembershipVersionKeyName = "DeploymentId";


        private static readonly string MembershipCollectionName = "OrleansMembership";

        public async Task InitMembershipVersionCollectionAsync(string deploymentId)
        {
            BsonDocument membershipVersionDocument = await this.FindDocumentAsync(MembershipVersionCollectionName, MembershipVersionKeyName, deploymentId);
            if (membershipVersionDocument == null)
            {
                membershipVersionDocument = new BsonDocument
                {
                    ["DeploymentId"] = deploymentId,
                    ["Timestamp"] = DateTime.UtcNow,
                    ["Version"] = 1
                };

                await this.SaveDocumentAsync(MembershipVersionCollectionName, MembershipVersionKeyName, deploymentId, membershipVersionDocument);
            }
        }

        public async Task<MembershipTableData> ReturnMembershipTableData(string deploymentId, string suspectingSilos)
        {
            if (string.IsNullOrEmpty(this.ConnectionString))
            {
                throw new ArgumentException("ConnectionString may not be empty");
            }

            var collection = this.ReturnOrCreateCollection(MembershipCollectionName);

            List<MembershipTable> membershipList = await Database.GetCollection<MembershipTable>(MembershipCollectionName).AsQueryable().ToListAsync();

            return await this.ReturnMembershipTableData(membershipList, deploymentId, suspectingSilos);
        }

        private async Task<MembershipTableData> ReturnMembershipTableData(List<MembershipTable> membershipList, string deploymentId, string suspectingSilos)
        {
            var membershipVersion = await this.FindDocumentAsync(MembershipVersionCollectionName, MembershipVersionKeyName, deploymentId);
            if (!membershipVersion.Contains("Version"))
            {
                membershipVersion["Version"] = 1;
            }

            var tableVersionEtag = membershipVersion["Version"].AsInt32;

            var membershipEntries = new List<Tuple<MembershipEntry, string>>();

            MembershipEntry entry;

            if (membershipList.Count > 0)
            {
                foreach (var membership in membershipList)
                {
                    membershipEntries.Add(new Tuple<MembershipEntry, string>(Parse(membership, suspectingSilos), string.Empty));
                }
            }

            return new MembershipTableData(membershipEntries, new TableVersion(tableVersionEtag, tableVersionEtag.ToString()));
        }

        private MembershipEntry Parse(MembershipTable entry, string suspectingSilos)
        {
            var parse = new MembershipEntry
            {
                HostName = entry.HostName,
                Status = (SiloStatus)entry.Status
            };

            parse.ProxyPort = entry.ProxyPort;

            parse.SiloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Parse(entry.Address), entry.Port), entry.Generation);

            // Todo: Why is there no SiloName?
            //if (!string.IsNullOrEmpty(siloName))
            //{
            //    parse.SiloName = tableEntry.SiloName;
            //}

            parse.StartTime = entry.StartTime;
            parse.IAmAliveTime = entry.IAmAliveTime;

            var suspectingSilosList = new List<SiloAddress>();
            var suspectingTimes = new List<DateTime>();

            if (!string.IsNullOrEmpty(suspectingSilos))
            {
                string[] silos = suspectingSilos.Split('|');
                foreach (string silo in silos)
                {
                    suspectingSilosList.Add(SiloAddress.FromParsableString(silo));
                }
            }

            if (!string.IsNullOrEmpty(entry.SuspectTimes))
            {
                string[] times = entry.SuspectTimes.Split('|');
                foreach (string time in times)
                    suspectingTimes.Add(ParseDate(time));
            }

            if (suspectingSilosList.Count != suspectingTimes.Count)
                throw new OrleansException(String.Format("SuspectingSilos.Length of {0} as read from Azure table is not eqaul to SuspectingTimes.Length of {1}", suspectingSilosList.Count, suspectingTimes.Count));

            for (int i = 0; i < suspectingSilosList.Count; i++)
                parse.AddSuspector(suspectingSilosList[i], suspectingTimes[i]);

            return parse;
        }

        public async Task<MembershipTableData> ReturnRow(SiloAddress key, string deploymentId, string suspectingSilos)
        {
            List<MembershipTable> membershipList = Database.GetCollection<MembershipTable>(MembershipCollectionName).AsQueryable().Where(m => m.DeploymentId == deploymentId && m.Address == key.Endpoint.Address.MapToIPv6().ToString() && m.Port == key.Endpoint.Port && m.Generation == key.Generation).ToList();
            return await this.ReturnMembershipTableData(membershipList, deploymentId, suspectingSilos);
        }

        public async Task<bool> InsertMembershipRow(string deploymentId, MembershipEntry entry, TableVersion tableVersion)
        {
            // Todo: Use async

            string address = entry.SiloAddress.Endpoint.Address.MapToIPv6().ToString();

            var collection = Database.GetCollection<MembershipTable>(MembershipCollectionName);

            var membershipDocument = collection.AsQueryable()
                    .FirstOrDefault(
                        m =>
                        m.DeploymentId == deploymentId
                        && m.Address == address
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

                var versionDocument = await this.FindDocumentAsync(MembershipVersionCollectionName, MembershipVersionKeyName, deploymentId);

                if (versionDocument != null)
                {
                    versionDocument["Version"] = versionDocument["Version"].AsInt32 + 1;
                    versionDocument["Timestamp"] = DateTime.UtcNow;

                    var builder = Builders<BsonDocument>.Filter.Eq(MembershipVersionKeyName, deploymentId);
                    await this.ReturnOrCreateCollection(MembershipVersionCollectionName).ReplaceOneAsync(builder, versionDocument);


                }
                else
                {
                    
                }
            }
            
            return true;
        }

        public async Task UpdateIAmAliveTimeAsyncTask(string deploymentId, SiloAddress siloAddress, DateTime iAmAliveTime)
        {
            var collection = Database.GetCollection<MembershipTable>(MembershipCollectionName);
            
            var update = new UpdateDefinitionBuilder<MembershipTable>().Set(x => x.IAmAliveTime, iAmAliveTime);
            var result = await collection.UpdateOneAsync(m =>
                m.DeploymentId == deploymentId && m.Address == siloAddress.Endpoint.Address.MapToIPv6().ToString()
                && m.Port == siloAddress.Endpoint.Port && m.Generation == siloAddress.Generation, update);

            var success = result.ModifiedCount == 1;
            
        }

        public MongoMembershipProviderRepository(string connectionsString, string databaseName)
            : base(connectionsString, databaseName)
        {
        }
    }
}
