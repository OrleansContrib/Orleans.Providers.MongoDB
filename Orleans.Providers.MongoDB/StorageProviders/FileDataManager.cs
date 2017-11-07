using System.IO;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;

namespace Orleans.Providers.MongoDB.StorageProviders
{
    /// <summary>
    ///     Interfaces with the file system.
    /// </summary>
    internal class FileDataManager : IJSONStateDataManager
    {
        private readonly DirectoryInfo directory;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="storageDirectory">A path relative to the silo host process' working directory.</param>
        public FileDataManager(string storageDirectory)
        {
            directory = new DirectoryInfo(storageDirectory);
            if (!directory.Exists)
                directory.Create();
        }

        /// <summary>
        ///     Deletes a file representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <returns>Completion promise for this operation.</returns>
        public Task Delete(string collectionName, string key)
        {
            var fileInfo = GetStorageFilePath(collectionName, key);

            if (fileInfo.Exists)
                fileInfo.Delete();

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Reads a file representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task<JObject> Read(string collectionName, string key)
        {
            var fileInfo = GetStorageFilePath(collectionName, key);

            if (!fileInfo.Exists)
                return null;

            using (var stream = fileInfo.OpenText())
            {
                var json = await stream.ReadToEndAsync();

                return JObject.Parse(json);
            }
        }

        /// <summary>
        ///     Writes a file representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <param name="entityData">The grain state data to be stored./</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task Write(string collectionName, string key, JObject entityData)
        {
            var fileInfo = GetStorageFilePath(collectionName, key);

            using (var stream = new StreamWriter(fileInfo.Open(FileMode.Create, FileAccess.Write)))
            {
                await stream.WriteAsync(entityData.ToJson());
            }
        }

        public void Dispose()
        {
        }

        /// <summary>
        ///     Returns the file path for storing that data with these keys.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <returns>File info for this storage data file.</returns>
        private FileInfo GetStorageFilePath(string collectionName, string key)
        {
            var fileName = key + "." + collectionName;
            var path = Path.Combine(directory.FullName, fileName);
            return new FileInfo(path);
        }
    }
}