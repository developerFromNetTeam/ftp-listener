using System.Threading.Tasks;

namespace windows_service_logic
{
    public class VideoFacade
    {
        private VideoConverter videoConverter;

        private AzureBlobStorageClient blobStorageClient;

        public VideoFacade()
        {
            this.videoConverter = new VideoConverter();
            this.blobStorageClient = new AzureBlobStorageClient();
        }

        public async Task Process(string filePath, string name)
        {
            var metadata = this.videoConverter.ProcessVideo(filePath, name);
            var azureFileUrl = await this.blobStorageClient.UploadFile(metadata.FilePath, metadata.FileName);
            this.videoConverter.DeleteVideoProcessDirectory(metadata.DirectoryPath);
        }
    }
}
