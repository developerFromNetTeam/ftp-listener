using System;
using System.Configuration;
using System.IO;
using System.Runtime.CompilerServices;
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

            var facade = new VideoFacade();
            var watcher = new FileSystemWatcher();
            watcher.Path = folderPath;
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                   | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Created += (sender, e) =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        WaitFileReady(e.FullPath);
                        await facade.Process(e.FullPath, e.Name);
                    }
                    catch (Exception ex)
                    {

                    }
                    //await fcmClient.SendNotificationAsync(new NotificationPayload
                        //{
                        //    To = File.ReadAllText(tokenFilePath),
                        //    Notification = new Notification
                        //    {
                        //        Title = "New video available.",
                        //        ClickAction = cloudBlockBlob.Uri.ToString(),
                        //        Icon = "https://sec-market.com.ua/889-large_default/dahua-dh-hac-hdw1000m-s3.jpg",
                        //        Body = "Size: " + (cloudBlockBlob.Properties.Length / 1048576).ToString("0.00 MB")
                        //    }
                        //});
                    });
            };

            watcher.EnableRaisingEvents = true;

            Console.ReadLine();
        }

        private static void WaitFileReady(string filePath)
        {
            while (IsFileLocked(filePath))
            {
                Task.Delay(1000);
            }
        }

        private static bool IsFileLocked(string filePath)
        {
            FileStream stream = null;

            try
            {
                stream = new FileInfo(filePath).Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
    }
}
