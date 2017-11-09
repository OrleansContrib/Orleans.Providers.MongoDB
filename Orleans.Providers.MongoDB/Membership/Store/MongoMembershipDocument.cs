using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Membership.Store
{
    public sealed class MongoMembershipDocument
    {
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }

        [BsonRequired]
        public MongoMembershipAddress SiloAddress { get; set; }

        [BsonRequired]
        public string Etag { get; set; }

        [BsonRequired]
        public string DeploymentId { get; set; }

        [BsonRequired]
        public string HostName { get; set; }

        [BsonRequired]
        public string SiloName { get; set; }

        [BsonRequired]
        public string RoleName { get; set; }

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

        [BsonRequired]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime IAmAliveTime { get; set; }

        [BsonRequired]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime StartTime { get; set; }

        public static MongoMembershipDocument Create(MembershipEntry entry, string deploymentId, string etag, string id)
        {
            var suspectTimes =
                entry.SuspectTimes?.Select(MongoSuspectTime.Create).ToList() ?? new List<MongoSuspectTime>();

            var siloAddress = MongoMembershipAddress.Create(entry.SiloAddress);

            return new MongoMembershipDocument
            {
                Id = id,
                DeploymentId = deploymentId,
                Etag = etag,
                FaultZone = entry.FaultZone,
                HostName = entry.HostName,
                IAmAliveTime = entry.IAmAliveTime,
                ProxyPort = entry.ProxyPort,
                RoleName = entry.RoleName,
                SiloAddress = siloAddress,
                SiloName = entry.SiloName,
                Status = (int)entry.Status,
                StartTime = entry.StartTime,
                SuspectTimes = suspectTimes,
                UpdateZone = entry.UpdateZone
            };
        }

        public MembershipEntry ToEntry()
        {
            return new MembershipEntry
            {
                FaultZone = FaultZone,
                HostName = HostName,
                IAmAliveTime = IAmAliveTime,
                ProxyPort = ProxyPort,
                RoleName = RoleName,
                SiloAddress = SiloAddress.ToSiloAddress(),
                SiloName = SiloName,
                Status = (SiloStatus) Status,
                StartTime = StartTime,
                SuspectTimes = SuspectTimes.Select(x => x.ToTuple()).ToList(),
                UpdateZone = UpdateZone
            };
        }
    }
}