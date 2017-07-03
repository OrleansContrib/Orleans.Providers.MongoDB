using Orleans.Providers.MongoDB.StorageProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Providers.MongoDB.Test.Host.CustomStorageProvider
{
    public class MongoDBStorageWithOrleansPrefix : MongoDBStorage
    {
        public override string ReturnGrainName(string grainType)
        {
            return string.Concat("OrleansStorage", base.ReturnGrainName(grainType));
        }
    }
}
