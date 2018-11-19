using System;

namespace windows_service_logic.Models
{
    public class UploadedVideoFileModel
    {
        public string Id{ get; set; }

        public string FilePath { get; set; }

        public string FileName { get; set; }

        public DateTime Date{ get; set; }
    }
}
