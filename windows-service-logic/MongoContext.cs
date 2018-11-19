using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using windows_service_logic.HashtableExtentions;
using windows_service_logic.Models;

namespace windows_service_logic
{
    public class MongoContext
    {
        private IMongoDatabase database;

        public MongoContext()
        {
            var mongodbconnection = ConfigurationSettings.AppSettings["mongodbconnection"];
            var client = new MongoClient(mongodbconnection);
            this.database = client.GetDatabase(ConfigurationSettings.AppSettings["mongodb"]);
        }

        public async Task<IEnumerable<ActiveSessionsData>> GetActiveSessionsData(string dvrName)
        {
            var collection = database.GetCollection<BsonDocument>("activeSessions");
            var cursor = collection.Find(new BsonDocument("DvrName", dvrName));

            var sessionData = await cursor.ToListAsync();
            var sessionDataHash = sessionData.Select(e => e.ToHashtable());
            return sessionDataHash.Select(x => x.ToActiveSessionsData()).ToList();

        }

        public async Task AddUploadedVideoFile(string fileName, string filePath)
        {
            var model = new UploadedVideoFileModel
            {
                Id = Guid.NewGuid().ToString(),
                FileName = fileName,
                FilePath = filePath.Substring(0, filePath.Length - fileName.Length),
                Date = DateTime.UtcNow
            };
            var collection = database.GetCollection<UploadedVideoFileModel>("uploadedVideoFiles");
            await collection.InsertOneAsync(model);
        }

        public async Task<IEnumerable<NotificationOption>> GetCamerasOptionsAsync(string dvrName)
        {
            var collection = database.GetCollection<BsonDocument>("dvrNotificationOptions");
            var cursor = collection.Find(new BsonDocument("DvrName", dvrName));

            var options = await cursor.ToListAsync();
            var optionsHash = options.Select(e => e.ToHashtable());
            var result = optionsHash.FirstOrDefault();

            if (result != null)
            {
                return result.ToCollection("NotificationOptions", x => x.ToNotificationOptions());
            }
            else
            {
                return new List<NotificationOption>();
            }
        }
    }
}
