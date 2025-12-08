using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;

namespace Orleans.Providers.MongoDB.UnitTest;

/// <summary>
/// Provides functionality to generate a unique fingerprint representation of a MongoDB query plan.
/// This fingerprint can be used for identifying and grouping similar queries within a database workload.
/// </summary>
internal static class MongoQueryPlanFingerprint
{
    private static readonly (string CommandType, Func<BsonDocument, IEnumerable<string>> Generator)[] Walkers = [
        ("find", document => WalkFilter(document["filter"].AsBsonDocument)),
        ("delete", document => WalkFilter(document["deletes"].AsBsonArray)),
        ("findAndModify", document => WalkFilter(document["query"].AsBsonDocument)),
        (
            "update",
            document => WalkNested(
                document["updates"].AsBsonArray, 
                innerDoc => WalkFilter(innerDoc["q"].AsBsonDocument)
            )
        ),
    ];

    /// <summary>
    /// Creates a unique fingerprint representation of a MongoDB query.
    /// </summary>
    /// <param name="query">The MongoDB query as a <see cref="BsonDocument"/> to generate a fingerprint for.</param>
    /// <returns>
    /// A string representing the fingerprint of the MongoDB query, or null if no matching fingerprint is generated.
    /// </returns>
    public static string CreateFingerprint(BsonDocument query)
    {
        return Walkers.Where(walker => query.Contains(walker.CommandType))
            .Select(walker => $"{walker.CommandType}={query[walker.CommandType].AsString}:{walker.Generator(query)}")
            .FirstOrDefault();
    }

    private static IEnumerable<string> WalkFilter(BsonValue seedDocument)
    {
        // we use a stack so we have a finite recursion cycle
        var sb = new StringBuilder();
        var stack = new Stack<(string[] Path, BsonValue Container)>([([], seedDocument)]);

        while (stack.TryPop(out var current))
        {
            switch (current.Container)
            {
                case BsonDocument doc:
                    foreach (var element in doc.Elements)
                    {
                        // step into and unpack the sub-document
                        stack.Push((current.Path.Concat([element.Name]).ToArray(), element.Value));
                    }

                    break;
                case BsonArray array:
                    int counter = 0;
                    foreach (var element in array)
                    {
                        // step into and unpack the array contents
                        stack.Push((current.Path.Concat([counter.ToString()]).ToArray(), element));
                        counter++;
                    }

                    break;
                default:
                    if (current.Path.Length > 0)
                    {
                        sb.AppendJoin('.', current.Path);
                        sb.Append('|');
                    }

                    break;
            }
        }
        
        return [sb.ToString()];
    }

    private static IEnumerable<string> WalkNested(BsonArray container, Func<BsonDocument, IEnumerable<string>> nested)
    {
        return container.SelectMany(item => nested(item.AsBsonDocument));
    }
}