using System.Linq;
using System.Threading.Tasks;

namespace windows_service_logic
{
    public class VideoFacade
    {
        private VideoConverter videoConverter;

        private AzureBlobStorageClient blobStorageClient;

        private FcmClient fcmClient;

        private MongoContext mongoContext;

        public VideoFacade()
        {
            this.videoConverter = new VideoConverter();
            this.blobStorageClient = new AzureBlobStorageClient();
            this.fcmClient = new FcmClient();
            this.mongoContext = new MongoContext();
        }

        public async Task Process(string filePath, string name)
        {
            var metadata = this.videoConverter.ParseMetadata(name);
            var cameraOptions = await this.mongoContext.GetCamerasOptionsAsync(metadata.DVRName);
            var isNotify = cameraOptions.FirstOrDefault(x => x.CameraSystemName == metadata.CameraName)?.IsNotificationEnable ?? false;

            metadata = this.videoConverter.ProcessVideo(filePath, name, metadata);
            var azureFileUrl = await this.blobStorageClient.UploadFile(metadata.FilePath, metadata.FileName);
            this.videoConverter.DeleteVideoProcessDirectory(metadata.DirectoryPath);
            if (isNotify)
            {
                var activeSessions = await this.mongoContext.GetActiveSessionsData(metadata.DVRName);
                var fcmMessage = new NotificationPayload
                {
                    Notification = new Notification
                    {
                        Title = cameraOptions.FirstOrDefault(x => x.CameraSystemName == metadata.CameraName)
                                    ?.CameraUserName ?? "None",
                        ClickAction = azureFileUrl,
                        Icon = "https://sec-market.com.ua/889-large_default/dahua-dh-hac-hdw1000m-s3.jpg",
                        Body = $"Length: {metadata.VideoLength}  Size: {metadata.VideoSizeMb}"
                    }
                };
                foreach (var activeSessionsData in activeSessions)
                {
                    fcmMessage.To = activeSessionsData.FcmToken;
                    await this.fcmClient.SendNotificationAsync(fcmMessage);
                }

            }
        }
    }
}
