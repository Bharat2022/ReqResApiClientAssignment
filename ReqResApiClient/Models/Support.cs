using System.Text.Json.Serialization;

namespace ReqResApiClient.Models
{
    public class Support
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}