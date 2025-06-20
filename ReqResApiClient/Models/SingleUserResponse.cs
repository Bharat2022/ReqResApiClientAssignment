using System.Text.Json.Serialization;

namespace ReqResApiClient.Models
{
    public class SingleUserResponse
    {
        [JsonPropertyName("data")]
        public User Data { get; set; }

        [JsonPropertyName("support")]
        public Support Support { get; set; }
    }
}