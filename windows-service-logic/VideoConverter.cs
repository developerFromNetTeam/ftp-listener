using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using windows_service_logic.Models;

namespace windows_service_logic
{
    public class VideoConverter
    {
        private string toolFolderPath;
        private string workerFolderPath;

        private const int SecondsToCutFromTheEnd = 6;
        private const int SecondsToLeaveForThePreRecordPart = 3;
        private const string InitialFileName = "video1.mp4";
        private const string FileNameWithCuttedEnd = "video-end-cut.mp4";
        private const string FileNameWithSplited = "video-end-cut-split.mp4";
        private const string FileNameWithSplitedPartOne = "video-end-cut-split-001.mp4";
        private const string FileNameWithSplitedPartTwo = "video-end-cut-split-002.mp4";
        private const string FileNameWithSplitedAndReEncoded = "video-end-cut-split-formated.mp4";
        private Logger logger;

        public VideoConverter()
        {
            this.toolFolderPath = ConfigurationSettings.AppSettings["toolFolderPath"];
            if (string.IsNullOrWhiteSpace(toolFolderPath))
            {
                throw new Exception("Tools folder path is empty.");
            }

            this.workerFolderPath = ConfigurationSettings.AppSettings["workerFolderPath"];
            if (string.IsNullOrWhiteSpace(workerFolderPath))
            {
                throw new Exception("Worker folder path is empty.");
            }

            this.logger = NLog.LogManager.GetCurrentClassLogger();
        }
        public VideoMetadata ProcessVideo(string path, string name, VideoMetadata metadata, string processFileId)
        {
            var processFolderName = $"{workerFolderPath}\\{Guid.NewGuid()}";
            this.logger.Info($"ProcessFileId:{processFileId}. Process folder name - {processFolderName}");

            //make mkv format from dav format.
            this.RunCommand(this.ConvertFromToDavToMkv(path, $"{processFolderName}\\{InitialFileName}"), output =>
            {
                this.logger.Info($"ProcessFileId:{processFileId}. Maked mkv format from dav format. Output: {output}");
            });

            //get initial video length.
            this.RunCommand(this.GetVideoLength($"{processFolderName}\\{InitialFileName}"), output0 =>
            {
                this.logger.Info($"ProcessFileId:{processFileId}. Got initial video length. Output:{output0}");
                var parseDuration1 = output0.Split(',')[0].Substring(12);

                var videoLength0 = TimeSpan.Parse(parseDuration1);

                //cut seconds from the end of video.
                this.RunCommand(this.CutVideo($"{processFolderName}\\{InitialFileName}",
                    $"{processFolderName}\\{FileNameWithCuttedEnd}", "00:00:00",
                    videoLength0.Subtract(TimeSpan.FromSeconds(SecondsToCutFromTheEnd)).ToString()), log =>
                {
                    this.logger.Info($"ProcessFileId:{processFileId}. Cut {SecondsToCutFromTheEnd} seconds from the end of video. Output: {log}");
                });

                // split video in two parts: pre-record part and main part.
                this.RunCommand(this.SplitVideo($"{processFolderName}\\{FileNameWithCuttedEnd}", $"{processFolderName}\\{FileNameWithSplited}", "00:00:05"),
                    log =>
                    {
                        this.logger.Info($"ProcessFileId:{processFileId}. Splited video in two parts: pre-record part and main part. Output: {log}");
                    });

                if (File.Exists($"{processFolderName}\\{FileNameWithSplitedPartTwo}"))
                {
                    // get length of pre-record part.
                    this.RunCommand(this.GetVideoLength($"{processFolderName}\\{FileNameWithSplitedPartOne}"),
                        output1 =>
                        {
                            var parseDuration = output1.Split(',')[0].Substring(12);
                            this.logger.Info($"ProcessFileId:{processFileId}. Got length of pre-record part. Output: {output1}");
                            var videoLength1 = TimeSpan.Parse(parseDuration);

                            // re-encode pre-record part in order to cut correctly, and cut it.
                            this.RunCommand(this.ReEncodeAndCutVideo(
                                $"{processFolderName}\\{FileNameWithSplitedPartOne}",
                                $"{processFolderName}\\{FileNameWithSplitedAndReEncoded}",
                                videoLength1.Subtract(TimeSpan.FromSeconds(SecondsToLeaveForThePreRecordPart))
                                    .ToString(), videoLength1.ToString()), log =>
                            {
                                this.logger.Info($"ProcessFileId:{processFileId}. Re-encoded pre-record part in order to cut correctly, and cut it. Output: {log}");
                            });

                            //merge two parts into result video.
                            this.RunCommand(this.MergeVideoParts(
                                $"{processFolderName}\\{FileNameWithSplitedAndReEncoded}",
                                $"{processFolderName}\\{FileNameWithSplitedPartTwo}",
                                $"{processFolderName}\\{metadata.FileName}"), log =>
                            {
                                this.logger.Info($"ProcessFileId:{processFileId}. Merged two parts into result video. Output: {log}");
                            });
                        });
                }
                else
                {
                    this.RunCommand(this.RenameFile($"{processFolderName}\\{FileNameWithCuttedEnd}", $"{metadata.FileName}"),
                        log =>
                        {
                            this.logger.Info($"ProcessFileId:{processFileId}. Renamed to result name. Output: {log}");
                        });
                }
            });

            this.RunCommand(this.GetVideoLength($"{processFolderName}\\{metadata.FileName}"), output =>
            {
                this.logger.Info($"ProcessFileId:{processFileId}. Got final video length. Output: {output}");
                var parseDuration = output.Split(',')[0].Substring(12);
                metadata.VideoLength = TimeSpan.Parse(parseDuration).ToString("hh':'mm':'ss");
            });

            var size = (new FileInfo($"{processFolderName}\\{metadata.FileName}").Length / 1024.0 / 1024.0).ToString("0.00");
            metadata.VideoSizeMb = $"{size} MB";
            metadata.FilePath = $"{processFolderName}\\{metadata.FileName}";
            metadata.DirectoryPath = processFolderName;

            return metadata;
        }

        public void DeleteVideoProcessDirectory(string path, string processFileId)
        {
            Directory.Delete(path, true);
            this.logger.Info($"ProcessFileId:{processFileId}. Deleted working folder - {path}");
        }

        public VideoMetadata ParseMetadata(string name)
        {
            var metadata = new VideoMetadata();

            var nameParts = name.Split('_');
            metadata.DVRName = nameParts[0].Split('\\').Last();
            metadata.CameraName = nameParts[1];
            metadata.IsMain = nameParts[2].Contains("main");

            metadata.FileName =
                $"{metadata.DVRName}_{metadata.CameraName}_{nameParts[3].Substring(0, 4)}-{nameParts[3].Substring(4, 2)}-{nameParts[3].Substring(6, 2)}--{nameParts[3].Substring(8, 2)}-{nameParts[3].Substring(10, 2)}-{nameParts[3].Substring(12, 2)}.mp4";
            return metadata;
        }

        #region private

        private DateTime ParseDateTime(string date)
        {
            return new DateTime(int.Parse(date.Substring(0, 4)), int.Parse(date.Substring(4, 2)), int.Parse(date.Substring(6, 2)), int.Parse(date.Substring(8, 2)), int.Parse(date.Substring(10, 2)), int.Parse(date.Substring(12, 2)));
        }

        private string RenameFile(string filePath, string newName)
        {
            return $"/C \"rename {filePath} {newName}\"";
        }

        private string ConvertFromToDavToMkv(string filePath, string newFilePath)
        {
            return $"/C \"{toolFolderPath}\\mkvmerge -o {newFilePath} {filePath}\"";
        }

        private string GetVideoLength(string filePath)
        {
            return $"/C \"{toolFolderPath}\\ffmpeg -i {filePath} 2>&1 | find \"Duration\"\"";
        }

        /// <summary>
        /// Split video.
        /// </summary>
        /// <param name="filePath">Video file path.</param>
        /// <param name="resultFilePath">Result video file path.</param>
        /// <param name="timestamps">Timestamps in format 00:00:00.</param>
        private string SplitVideo(string filePath, string resultFilePath, string timestamps)
        {
            return $"/C \"{toolFolderPath}\\mkvmerge -o {resultFilePath} --split timestamps:{timestamps} {filePath}\"";
        }

        /// <summary>
        /// Cut video.
        /// </summary>
        /// <param name="filePath">Video file path.</param>
        /// <param name="resultFilePath">Result video file path.</param>
        /// <param name="startTime">Start time in 00:00:00 format.</param>
        /// <param name="endTime">End time in 00:00:00 format.</param>
        private string CutVideo(string filePath, string resultFilePath, string startTime, string endTime)
        {
            return $"/C \"{toolFolderPath}\\ffmpeg -ss {startTime} -i {filePath} -to {endTime} -c copy {resultFilePath}\"";
        }

        /// <summary>
        /// Cut and re-encode video.
        /// </summary>
        /// <param name="filePath">Video file path.</param>
        /// <param name="resultFilePath">Result video file path.</param>
        /// <param name="startTime">Start time in 00:00:00 format.</param>
        /// <param name="endTime">End time in 00:00:00 format.</param>
        private string ReEncodeAndCutVideo(string filePath, string resultFilePath, string startTime, string endTime)
        {
            return $"/C \"{toolFolderPath}\\ffmpeg -i {filePath} -ss {startTime} -to {endTime} -async 1 {resultFilePath}\"";
        }

        private string MergeVideoParts(string file1, string file2, string resultPath)
        {
            return $"/C \"{toolFolderPath}\\mkvmerge -o {resultPath} {file1} +{file2}\"";
        }

        private void RunCommand(string command, Action<string> nextActionWithOutput = null)
        {
            var psi = new ProcessStartInfo("cmd.exe")
            {
                Arguments = command,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using (var process = Process.Start(psi))
            {
                string output = null;
                if (nextActionWithOutput != null)
                {
                    output = process.StandardOutput.ReadToEnd();
                }
                process.WaitForExit();
                if (nextActionWithOutput != null)
                {
                    nextActionWithOutput(output);
                }
            }
        }
        #endregion
    }
}
