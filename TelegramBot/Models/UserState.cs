namespace TelegramBotApp.Models;

public class UserState
{
    public long Id { get; set; }
    public long TelegramUserId { get; set; }
    public string CurrentFlow { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public string? EntityId { get; set; }
    public string? CollectedData { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? LastBotMessageId { get; set; }
    public long? SelectedGiftId { get; set; }
}
