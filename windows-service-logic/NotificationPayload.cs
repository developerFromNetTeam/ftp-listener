using Newtonsoft.Json;

namespace windows_service_logic
{
    public class NotificationPayload
    {
        [JsonProperty(PropertyName = "to")]
        public string To { get; set; }

        [JsonProperty(PropertyName = "notification")]
        public Notification Notification { get; set; }
    }
}
