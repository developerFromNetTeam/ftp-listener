using Newtonsoft.Json;

namespace windows_service_logic
{
    public class Notification
    {
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "body")]
        public string Body { get; set; }

        [JsonProperty(PropertyName = "icon")]
        public string Icon { get; set; }

        [JsonProperty(PropertyName = "click_action")]
        public string ClickAction { get; set; }
    }
}
