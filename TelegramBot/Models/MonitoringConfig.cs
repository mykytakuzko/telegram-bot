using System.Text.Json.Serialization;

namespace TelegramBotApp.Models;

public class MonitoringConfig
{
    [JsonPropertyName("gift_name")]
    public string GiftName { get; set; } = string.Empty;
    
    [JsonPropertyName("account_interval")]
    public int AccountInterval { get; set; }
    
    [JsonPropertyName("max_batches")]
    public int MaxBatches { get; set; }
    
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
    
    [JsonPropertyName("accounts")]
    public List<MonitoringAccount> Accounts { get; set; } = new();
}

public class MonitoringAccount
{
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }
    
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
}
