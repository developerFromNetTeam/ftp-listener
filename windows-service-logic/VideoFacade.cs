using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace windows_service_logic
{
    public class VideoFacade
    {
        private VideoConverter videoConverter;

        private AzureBlobStorageClient blobStorageClient;

        private FcmClient fcmClient;

        private MongoContext mongoContext;

        private Logger logger;

        public VideoFacade()
        {
            this.videoConverter = new VideoConverter();
            this.blobStorageClient = new AzureBlobStorageClient();
            this.fcmClient = new FcmClient();
            this.mongoContext = new MongoContext();
            this.logger = NLog.LogManager.GetCurrentClassLogger();
        }

        public async Task Process(string filePath, string name)
        {
            var processFileId = Guid.NewGuid().ToString();
            try
            {
                this.logger.Info($"ProcessFileId:{processFileId}. Started processing of {name}.");

                var metadata = this.videoConverter.ParseMetadata(name);

                this.logger.Info($"ProcessFileId:{processFileId}. Parsed first part of metadata - {metadata}");

                var cameraOptions = await this.mongoContext.GetCamerasOptionsAsync(metadata.DVRName);
                this.logger.Info($"ProcessFileId:{processFileId}. Get cameras options - {JsonConvert.SerializeObject(cameraOptions)}");

                var isNotify = cameraOptions.FirstOrDefault(x => x.CameraSystemName == metadata.CameraName)
                                   ?.IsNotificationEnable ?? false;

                this.logger.Info($"ProcessFileId:{processFileId}. Started converter.");
                metadata = this.videoConverter.ProcessVideo(filePath, name, metadata, processFileId);
                this.logger.Info($"ProcessFileId:{processFileId}. Parsed second part of metadata - {metadata}");

                var azureFileUrl = await this.blobStorageClient.UploadFile(metadata.FilePath, metadata.FileName);
                this.logger.Info($"ProcessFileId:{processFileId}. Uploaded to azure - {azureFileUrl}");

                this.videoConverter.DeleteVideoProcessDirectory(metadata.DirectoryPath, processFileId);

                if (isNotify)
                {
                    var activeSessions = await this.mongoContext.GetActiveSessionsData(metadata.DVRName);
                    this.logger.Info($"ProcessFileId:{processFileId}. Active sessions to send notification - {JsonConvert.SerializeObject(activeSessions.Select(x => x.UserId))}");

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

                    this.logger.Info($"ProcessFileId:{processFileId}. Prepared object to send notification - {JsonConvert.SerializeObject(fcmMessage)}");

                    foreach (var activeSessionsData in activeSessions)
                    {
                        this.logger.Info($"ProcessFileId:{processFileId}. Sending notification to - {activeSessionsData.UserId}");
                        fcmMessage.To = activeSessionsData.FcmToken;
                        var result = await this.fcmClient.SendNotificationAsync(fcmMessage);
                        this.logger.Info($"ProcessFileId:{processFileId}. Sent notification to - {activeSessionsData.UserId}. Response: {result}");
                    }

                }
                this.logger.Info($"ProcessFileId:{processFileId}. Finished precessing.");
            }
            catch (Exception ex)
            {
                this.logger.Error($"ProcessFileId:{processFileId}. Error message: {ex.Message}.");
            }
        }
    }
}
