using System.Collections;
using windows_service_logic.Models;

namespace windows_service_logic.HashtableExtentions
{
    public static class NotificationOptionsConverter
    {
        public static NotificationOption ToNotificationOptions(this Hashtable hashtable)
        {
            return new NotificationOption
            {
                IsNotificationEnable = bool.Parse(hashtable["IsNotificationEnable"].ToString()),
                CameraSystemName = hashtable["CameraSystemName"].ToString(),
                CameraUserName = hashtable["CameraUserName"].ToString()
            };
        }
    }
}
