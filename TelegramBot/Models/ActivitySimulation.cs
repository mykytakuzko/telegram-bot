using System.Text.Json.Serialization;

namespace TelegramBotApp.Models;

public class ActivitySimulationConfig
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

    [JsonPropertyName("activitiesToday")]
    public int ActivitiesToday { get; set; }

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

    [JsonPropertyName("nextActivityAt")]
    public DateTime? NextActivityAt { get; set; }

    [JsonPropertyName("lastActivityAt")]
    public DateTime? LastActivityAt { get; set; }

    [JsonPropertyName("targets")]
    public List<ActivitySimulationTarget>? Targets { get; set; }
}

public class ActivitySimulationTarget
{
    [JsonPropertyName("chatUsername")]
    public string ChatUsername { get; set; } = string.Empty;

    [JsonPropertyName("targetType")]
    public string TargetType { get; set; } = "CHANNEL";

    [JsonPropertyName("canRead")]
    public bool CanRead { get; set; } = true;

    [JsonPropertyName("canLike")]
    public bool CanLike { get; set; } = true;

    [JsonPropertyName("canSend")]
    public bool CanSend { get; set; } = false;
}

public class ActivitySimulationHistory
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("activityType")]
    public string ActivityType { get; set; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("executedAt")]
    public DateTime ExecutedAt { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}

public class MessageTemplate
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("templateText")]
    public string TemplateText { get; set; } = string.Empty;

    [JsonPropertyName("usageCount")]
    public int UsageCount { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("lastUsedAt")]
    public DateTime? LastUsedAt { get; set; }
}

public class CreateActivitySimulationRequest
{
    [JsonPropertyName("userId")]
    public long UserId { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("pauseDuringMonitoring")]
    public bool PauseDuringMonitoring { get; set; } = true;

    [JsonPropertyName("minActivitiesPerDay")]
    public int MinActivitiesPerDay { get; set; } = 5;

    [JsonPropertyName("maxActivitiesPerDay")]
    public int MaxActivitiesPerDay { get; set; } = 15;

    [JsonPropertyName("minIntervalMinutes")]
    public int MinIntervalMinutes { get; set; } = 10;

    [JsonPropertyName("maxIntervalMinutes")]
    public int MaxIntervalMinutes { get; set; } = 120;

    [JsonPropertyName("readChannelWeight")]
    public int ReadChannelWeight { get; set; } = 40;

    [JsonPropertyName("likePostWeight")]
    public int LikePostWeight { get; set; } = 30;

    [JsonPropertyName("sendMessageWeight")]
    public int SendMessageWeight { get; set; } = 30;

    [JsonPropertyName("targets")]
    public List<ActivitySimulationTarget>? Targets { get; set; }
}
