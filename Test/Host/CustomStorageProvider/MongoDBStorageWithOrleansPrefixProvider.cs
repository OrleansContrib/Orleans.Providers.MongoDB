using Orleans.Providers.MongoDB.StorageProviders;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Test.Host.CustomStorageProvider
{
    public class MongoDBStorageWithOrleansPrefix : MongoStorageProvider
    {
        public override string ReturnGrainName(string grainType, GrainReference grainReference)
        {
            return string.Concat("OrleansStorage", base.ReturnGrainName(grainType, grainReference));
        }
    }
}