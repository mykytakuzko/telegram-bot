using System.Text.Json.Serialization;

namespace TelegramBotApp.Models;

public class MonitoringConfigsResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public List<MonitoringConfig> Data { get; set; } = new();
}

public class MonitoringConfigResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public MonitoringConfig? Data { get; set; }
}

public class MonitoringConfig
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("gift_id")]
    public long GiftId { get; set; }
    
    [JsonPropertyName("gift_name")]
    public string GiftName { get; set; } = string.Empty;
    
    [JsonPropertyName("account_interval")]
    public int AccountInterval { get; set; }
    
    [JsonPropertyName("max_batches")]
    public int MaxBatches { get; set; }
    
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [JsonPropertyName("accounts")]
    public List<MonitoringAccount> Accounts { get; set; } = new();
}

public class MonitoringAccount
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("gift_name")]
    public string? GiftName { get; set; }
    
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }
    
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
}
