using System.Text.Json.Serialization;

namespace TelegramBotApp.Models;

public class ResoldGiftOrder
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }
    
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
    
    [JsonPropertyName("min_price")]
    public int MinPrice { get; set; }
    
    [JsonPropertyName("max_price")]
    public int MaxPrice { get; set; }
    
    [JsonPropertyName("amount_to_buy")]
    public int AmountToBuy { get; set; }

    [JsonPropertyName("amount_bought")]
    public int AmountBought { get; set; }
    
    [JsonPropertyName("model_name")]
    public string? ModelName { get; set; }
    
    [JsonPropertyName("symbol_name")]
    public string? SymbolName { get; set; }
    
    [JsonPropertyName("backdrop_name")]
    public string? BackdropName { get; set; }
    
    [JsonPropertyName("gift_name")]
    public string GiftName { get; set; } = string.Empty;
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;
    
    [JsonPropertyName("is_only_ton_payment")]
    public bool IsOnlyTonPayment { get; set; }
    
    [JsonPropertyName("should_buy_with_original_details")]
    public bool ShouldBuyWithOriginalDetails { get; set; }
}
