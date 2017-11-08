using System.IO;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    internal sealed class FileDataManager : IJSONStateDataManager
    {
        private readonly DirectoryInfo directory;

        public FileDataManager(string storageDirectory)
        {
            directory = new DirectoryInfo(storageDirectory);

            if (!directory.Exists)
            {
                directory.Create();
            }
        }

        public Task Delete(string collectionName, string key)
        {
            var fileInfo = GetStorageFilePath(collectionName, key);

            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }

            return Task.CompletedTask;
        }

        public async Task<(string Etag, JObject Value)> Read(string collectionName, string key)
        {
            var fileInfo = GetStorageFilePath(collectionName, key);

            if (!fileInfo.Exists)
            {
                return (null, null);
            }

            using (var stream = fileInfo.OpenText())
            {
                var json = await stream.ReadToEndAsync();

                return (null, JObject.Parse(json));
            }
        }
        
        public async Task<string> Write(string collectionName, string key, JObject entityData, string etag)
        {
            var fileInfo = GetStorageFilePath(collectionName, key);

            using (var stream = new StreamWriter(fileInfo.Open(FileMode.Create, FileAccess.Write)))
            {
                await stream.WriteAsync(entityData.ToJson());

                return null;
            }
        }

        public void Dispose()
        {
        }
        
        private FileInfo GetStorageFilePath(string collectionName, string key)
        {
            var fileName = key + "." + collectionName;
            var filePath = Path.Combine(directory.FullName, fileName);

            return new FileInfo(filePath);
        }
    }
}