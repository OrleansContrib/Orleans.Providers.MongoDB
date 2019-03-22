using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Orleans.Runtime;
using SiloAddressClass = Orleans.Runtime.SiloAddress;

namespace Orleans.Providers.MongoDB.Membership.Store
{
    public sealed class MongoMembershipDocument
    {
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }

        [BsonRequired]
        public string Etag { get; set; }

        [BsonRequired]
        public string DeploymentId { get; set; }

        [BsonRequired]
        public string HostName { get; set; }

        [BsonRequired]
        public string SiloAddress { get; set; }

        [BsonRequired]
        public string SiloName { get; set; }

        [BsonRequired]
        public string RoleName { get; set; }

        [BsonIgnoreIfNull]
        public string StatusText { get; set; }

        [BsonRequired]
        public string IAmAliveTime { get; set; }

        [BsonRequired]
        public string StartTime { get; set; }

        [BsonRequired]
        public int ProxyPort { get; set; }

        [BsonRequired]
        public int UpdateZone { get; set; }

        [BsonRequired]
        public int FaultZone { get; set; }

        [BsonRequired]
        public int Status { get; set; }

        [BsonRequired]
        public List<MongoSuspectTime> SuspectTimes { get; set; }

        [BsonIgnoreIfDefault]
        public DateTime Timestamp { get; set; }

        public static MongoMembershipDocument Create(MembershipEntry entry, string deploymentId, string etag, string id)
        {
            var suspectTimes =
                entry.SuspectTimes?.Select(MongoSuspectTime.Create).ToList() ?? new List<MongoSuspectTime>();

            return new MongoMembershipDocument
            {
                Id = id,
                DeploymentId = deploymentId,
                Etag = etag,
                FaultZone = entry.FaultZone,
                HostName = entry.HostName,
                IAmAliveTime = LogFormatter.PrintDate(entry.IAmAliveTime),
                ProxyPort = entry.ProxyPort,
                RoleName = entry.RoleName,
                SiloAddress = entry.SiloAddress.ToParsableString(),
                SiloName = entry.SiloName,
                Status = (int)entry.Status,
                StatusText = entry.Status.ToString(),
                StartTime = LogFormatter.PrintDate(entry.StartTime),
                SuspectTimes = suspectTimes,
                Timestamp = entry.IAmAliveTime,
                UpdateZone = entry.UpdateZone
            };
        }

        public MembershipEntry ToEntry()
        {
            return new MembershipEntry
            {
                FaultZone = FaultZone,
                HostName = HostName,
                IAmAliveTime = LogFormatter.ParseDate(IAmAliveTime),
                ProxyPort = ProxyPort,
                RoleName = RoleName,
                SiloAddress = SiloAddressClass.FromParsableString(SiloAddress),
                SiloName = SiloName,
                Status = (SiloStatus) Status,
                StartTime = LogFormatter.ParseDate(StartTime),
                SuspectTimes = SuspectTimes.Select(x => x.ToTuple()).ToList(),
                UpdateZone = UpdateZone
            };
        }
    }
}