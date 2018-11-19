using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using LogLevel = NLog.LogLevel;

namespace windows_service_logic
{
    public class Worker
    {
        public static void Run()
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            try
            {
                logger.Info($"Started worker at {DateTime.UtcNow.ToString()}");

                var folderPath = ConfigurationSettings.AppSettings["folderPath"];

                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    throw new Exception("Folder path is empty");
                }

                var facade = new VideoFacade();
                var watcher = new FileSystemWatcher();
                watcher.Path = folderPath;
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                       | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.Created += (sender, e) =>
                {
                    if (e.Name.Contains(".dav"))
                    {
                        logger.Info($"New notify by path: {e.FullPath}");
                        Task.Run(async () =>
                        {
                            logger.Info($"Waiting copying file: {e.FullPath}");
                            WaitFileReady(e.FullPath);
                            logger.Info($"File is copied: {e.FullPath}");
                            await facade.Process(e.FullPath, e.Name);

                        });
                    }
                    else
                    {
                        logger.Warn($"Not supported file type: {e.Name}");
                    }
                };

                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                logger.Error($"Worker error: {ex.Message}");
            }
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
