using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace windows_service_logic
{
    public class AzureBlobStorageClient
    {
        private CloudStorageAccount StorageAccount;
        private CloudBlobContainer BlobContainer;

        public AzureBlobStorageClient()
        {
            var connectionString = ConfigurationSettings.AppSettings["connectionString"];
            var blobContainer = ConfigurationSettings.AppSettings["container"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("Azure blob storage connection string is empty.");
            }
            if (string.IsNullOrWhiteSpace(blobContainer))
            {
                throw new Exception("Azure blob storage container name is empty.");
            }

            if (CloudStorageAccount.TryParse(connectionString, out this.StorageAccount) && this.StorageAccount != null)
            {
                var cloudBlobClient = this.StorageAccount.CreateCloudBlobClient();
                this.BlobContainer = cloudBlobClient.GetContainerReference(blobContainer);
            }
            else
            {
                throw new Exception("Error parsing cloud storage account from connection string.");
            }
        }

        public async Task<string> UploadFile(string fullPath, string name)
        {
            var cloudBlockBlob = this.BlobContainer.GetBlockBlobReference(name);
            cloudBlockBlob.Properties.ContentType = "video/mp4";
                //System.Web.MimeMapping.GetMimeMapping(fullPath);
            await cloudBlockBlob.UploadFromFileAsync(fullPath);
            await cloudBlockBlob.FetchAttributesAsync();
            return cloudBlockBlob.Uri.ToString();
        }
    }
}
