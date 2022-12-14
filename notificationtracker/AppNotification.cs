using System;
using Newtonsoft.Json;

namespace notificationtracker
{
    public class AppNotificationText
    {
        [JsonProperty("extra_title")]
        public string ExtraTitle { set; get; }

        [JsonProperty("extra_titlebig")]
        public string ExtraTitleBig { set; get; }

        [JsonProperty("extra_text")]
        public string ExtraText { set; get; }

        [JsonProperty("extra_subtext")]
        public string ExtraSubText { set; get; }

        [JsonProperty("extra_summarytext")]
        public string ExtraSummaryText { set; get; }

        [JsonProperty("extra_bigtext")]
        public string ExtraBigText { set; get; }

        [JsonProperty("extra_infotext")]
        public string ExtraInfoText { set; get; }

        [JsonProperty("ticker")]
        public string Ticker { set; get; }
    }

    public class AppNotification
    {
        [JsonProperty("type")]
        public string Type { set; get; }

        [JsonProperty("package_app")]
        public string Package { set; get; }

        [JsonProperty("content")]
        public AppNotificationText Content { set; get; }

        [JsonProperty("created_at")]
        public string CreateAt { set; get; }

        public string Guuid { set; get; }

        public AppNotification()
        {
            CreateAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Type = "notification";
            Guuid = Guid.NewGuid().ToString();
        }
    }
}