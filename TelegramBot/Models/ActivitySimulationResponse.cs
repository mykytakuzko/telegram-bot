using System.Text.Json.Serialization;

namespace TelegramBotApp.Models;

public class ActivitySimulationResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("userId")]
    public long UserId { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("pauseDuringMonitoring")]
    public bool PauseDuringMonitoring { get; set; }

    [JsonPropertyName("minActivitiesPerDay")]
    public int MinActivitiesPerDay { get; set; }

    [JsonPropertyName("maxActivitiesPerDay")]
    public int MaxActivitiesPerDay { get; set; }

    [JsonPropertyName("minIntervalMinutes")]
    public int MinIntervalMinutes { get; set; }

    [JsonPropertyName("maxIntervalMinutes")]
    public int MaxIntervalMinutes { get; set; }

    [JsonPropertyName("readChannelWeight")]
    public int ReadChannelWeight { get; set; }

    [JsonPropertyName("likePostWeight")]
    public int LikePostWeight { get; set; }

    [JsonPropertyName("sendMessageWeight")]
    public int SendMessageWeight { get; set; }

    [JsonPropertyName("activitiesToday")]
    public int ActivitiesToday { get; set; }

    [JsonPropertyName("targets")]
    public List<ActivitySimulationTarget>? Targets { get; set; }
}
