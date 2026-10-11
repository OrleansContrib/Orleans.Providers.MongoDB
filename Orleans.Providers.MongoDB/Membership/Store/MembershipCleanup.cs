using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Membership.Store
{
    internal static class MembershipCleanup
    {
        private static readonly FilterDefinitionBuilder<MongoMembershipDocument> Filter = Builders<MongoMembershipDocument>.Filter;

        /// <summary>
        /// Deletes the defunct silo entries of a deployment from a collection that stores one document per silo.
        /// </summary>
        public static async Task DeleteDefunctSiloEntriesAsync(
            IMongoCollection<MongoMembershipDocument> collection,
            string deploymentId,
            DateTimeOffset beforeDate,
            CancellationToken cancellationToken)
        {
            var beforeUtc = beforeDate.UtcDateTime;

            var deadEntries =
                await collection.Find(x => x.DeploymentId == deploymentId && x.Status == (int)SiloStatus.Dead)
                    .ToListAsync(cancellationToken);

            var defunctEntries = deadEntries.Where(x => x.IsDefunct(beforeUtc)).ToList();

            if (defunctEntries.Count == 0)
            {
                return;
            }

            // Only delete the revisions that were evaluated, so that concurrent updates are never lost.
            // Heartbeats change IAmAliveTime without assigning a new etag, so both are part of the precondition.
            await collection.DeleteManyAsync(
                Filter.Or(defunctEntries.Select(x =>
                    Filter.Eq(d => d.Id, x.Id) &
                    Filter.Eq(d => d.Etag, x.Etag) &
                    Filter.Eq(d => d.IAmAliveTime, x.IAmAliveTime))),
                cancellationToken);
        }
    }
}
