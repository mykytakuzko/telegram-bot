using System.Text.Json.Serialization;

namespace TelegramBot.Models
{
    public class User
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("telegramUserId")]
        public long TelegramUserId { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }
    }
}
