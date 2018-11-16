using System.Collections;
using windows_service_logic.Models;

namespace windows_service_logic.HashtableExtentions
{
    public static class ActiveSessionsDataConverter
    {
        public static ActiveSessionsData ToActiveSessionsData(this Hashtable hashtable)
        {
            return new ActiveSessionsData
            {
                FcmToken = hashtable["FcmToken"].ToString()
            };
        }
    }
}
