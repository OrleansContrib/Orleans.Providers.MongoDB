using Orleans.Providers.MongoDB.StorageProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Providers.MongoDB.Test.Host.CustomStorageProvider
{
    public class MongoDBStorageWithOrleansPrefix : MongoDBStorage
    {
        public override string ReturnGrainName(string grainType, GrainReference grainReference)
        {
            return string.Concat("OrleansStorage", base.ReturnGrainName(grainType, grainReference));
        }
    }
}
