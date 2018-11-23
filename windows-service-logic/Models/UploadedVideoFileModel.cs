using System;

namespace windows_service_logic.Models
{
    public class UploadedVideoFileModel
    {
        public string Id { get; set; }

        public string FilePath { get; set; }

        public string FileName { get; set; }

        public string CameraName { get; set; }

        public string DvrName { get; set; }

        /// <summary>
        /// Utc datetime of adding to db.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// local datetime of video event
        /// </summary>
        public DateTime VideoStartDateLocal { get; set; }
    }
}
