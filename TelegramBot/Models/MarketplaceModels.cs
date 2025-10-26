namespace TelegramBotApp.Models;

public class GiftsResponse
{
    public List<GiftItem> Gifts { get; set; } = new();
}

public class GiftItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class GiftModelsResponse
{
    public List<GiftModelItem> GiftModels { get; set; } = new();
}

public class GiftModelItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long StickerId { get; set; }
}

public class GiftSymbolsResponse
{
    public List<GiftSymbolItem> GiftSymbols { get; set; } = new();
}

public class GiftSymbolItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long StickerId { get; set; }
}

public class GiftBackdropsResponse
{
    public List<GiftBackdropItem> GiftBackdrops { get; set; } = new();
}

public class GiftBackdropItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BackdropId { get; set; }
}
