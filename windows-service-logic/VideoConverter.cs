using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace windows_service_logic
{
    public class VideoConverter
    {
        private string toolFolderPath = ConfigurationSettings.AppSettings["toolFolderPath"];
        private string workerFolderPath = ConfigurationSettings.AppSettings["workerFolderPath"];
        private Dictionary<string, string> cameraNameDictionary = new Dictionary<string, string>
        {
            {"ch3","Kitchen"}
        };

        private const int SecondsToCutFromTheEnd = 8;
        private const int SecondsToLeaveForThePreRecordPart = 3;
        private const string InitialFileName = "video1.mp4";
        private const string FileNameWithCuttedEnd = "video-end-cut.mp4";
        private const string FileNameWithSplited = "video-end-cut-split.mp4";
        private const string FileNameWithSplitedPartOne = "video-end-cut-split-001.mp4";
        private const string FileNameWithSplitedPartTwo = "video-end-cut-split-002.mp4";
        private const string FileNameWithSplitedAndReEncoded = "video-end-cut-split-formated.mp4";
        private const string ResultFileName = "RESULT.mp4";
        public string ProcessVideo(string path, string name)
        {
            var metadata = this.ParseMetadata(name);
            //make mkv format from dav format.
            this.RunCommand(this.ConvertFromToDavToMkv(path));

            //get initial video length.
            this.RunCommand(this.GetVideoLength($"{workerFolderPath}\\{InitialFileName}"), output0 =>
            {
                var videoLength0 = TimeSpan.Parse(output0);
                //cut 9 seconds from the end of video.
                this.RunCommand(this.CutVideo($"{workerFolderPath}\\{InitialFileName}",
                    $"{workerFolderPath}\\{FileNameWithCuttedEnd}", "00:00:00",
                    videoLength0.Subtract(TimeSpan.FromSeconds(SecondsToCutFromTheEnd)).ToString()));

                // split video in two parts: pre-record part and main part.
                this.RunCommand(this.SplitVideo($"{workerFolderPath}\\{FileNameWithCuttedEnd}", $"{workerFolderPath}\\{FileNameWithSplited}", "00:00:05"));

                // get length of pre-record part.
                this.RunCommand(this.GetVideoLength($"{workerFolderPath}\\{FileNameWithSplitedPartOne}"),
                    output1 =>
                    {
                        var videoLength1 = TimeSpan.Parse(output1);

                        // re-encode pre-record part in order to cut correctly, and cut it.
                        this.RunCommand(this.ReEncodeAndCutVideo(
                            $"{workerFolderPath}\\{FileNameWithSplitedPartOne}",
                            $"{workerFolderPath}\\{FileNameWithSplitedAndReEncoded}",
                            videoLength1.Subtract(TimeSpan.FromSeconds(SecondsToLeaveForThePreRecordPart)).ToString(), videoLength1.ToString()));

                        //merge two parts into result video.
                        this.RunCommand(this.MergeVideoParts($"{workerFolderPath}\\{FileNameWithSplitedAndReEncoded}", $"{workerFolderPath}\\{FileNameWithSplitedPartTwo}", $"{workerFolderPath}\\{ResultFileName}"));
                    });
            });
            return string.Empty;
        }

        private VideoMetadata ParseMetadata(string name)
        {
            var metadata = new VideoMetadata();

            var nameParts = name.Split('_');
            metadata.DVRName = nameParts[0];
            metadata.CameraName = cameraNameDictionary[nameParts[1]];
            metadata.IsMain = nameParts[2].Contains("main");
            metadata.StarTime = this.ParseDateTime(nameParts[3]).AddSeconds(-11);
            metadata.EndTime = this.ParseDateTime(nameParts[4]);
            return metadata;
        }

        private DateTime ParseDateTime(string date)
        {
            return new DateTime(int.Parse(date.Substring(0, 4)), int.Parse(date.Substring(4, 2)), int.Parse(date.Substring(6, 2)), int.Parse(date.Substring(8, 2)), int.Parse(date.Substring(10, 2)), int.Parse(date.Substring(12, 2)));
        }

        private string ConvertFromToDavToMkv(string filePath)
        {
            return $"/C \"{toolFolderPath}\\mkvmerge -o {workerFolderPath}\\{InitialFileName} {filePath}\"";
        }

        private string GetVideoLength(string filePath)
        {
            return $"/C \"{toolFolderPath}\\ffmpeg -i {filePath} 2>&1 | grep Duration | cut -d ' ' -f 4 | sed s/,//\"";
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
    }

    public class VideoMetadata
    {
        public string DVRName { get; set; }

        public string CameraName { get; set; }

        public bool IsMain { get; set; }

        public DateTime StarTime { get; set; }

        public DateTime EndTime { get; set; }

        public TimeSpan VideoLength
        {
            get { return EndTime - StarTime; }
        }

    }
}
