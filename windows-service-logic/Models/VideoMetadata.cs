using System;

namespace windows_service_logic.Models
{
    public class VideoMetadata
    {
        public string DVRName { get; set; }

        public string CameraName { get; set; }

        public bool IsMain { get; set; }

        public string VideoLength { get; set; }

        public string VideoSizeMb { get; set; }

        public string DirectoryPath { get; set; }

        public string FilePath { get; set; }

        public string FileName { get; set; }
    }
}
