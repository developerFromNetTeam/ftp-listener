using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace windows_service_logic
{
    public class Worker
    {
        public static void Run()
        {
            var folderPath = ConfigurationSettings.AppSettings["folderPath"];
            var connectionString = ConfigurationSettings.AppSettings["connectionString"];
            var blobContainer = ConfigurationSettings.AppSettings["container"];

            CloudStorageAccount storageAccount = null;
            if (CloudStorageAccount.TryParse(connectionString, out storageAccount) && storageAccount != null)
            {
                var cloudBlobClient = storageAccount.CreateCloudBlobClient();
                var container = cloudBlobClient.GetContainerReference(blobContainer);

                var watcher = new FileSystemWatcher();
                watcher.Path = folderPath;
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                       | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.Created += (sender, e) =>
                {
                    Task.Run(async () =>
                    {
                        var cloudBlockBlob = container.GetBlockBlobReference(e.Name);
                        cloudBlockBlob.Properties.ContentType = MimeMapping.GetMimeMapping(e.FullPath);
                        
                        await cloudBlockBlob.UploadFromFileAsync(e.FullPath);
                    });
                };

                watcher.EnableRaisingEvents = true;

                Console.ReadLine();
            }
            else
            {

            }
        }
    }
}
