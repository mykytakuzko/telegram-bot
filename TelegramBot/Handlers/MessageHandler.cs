using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotApp.Models;
using TelegramBotApp.Services;

namespace TelegramBotApp.Handlers;

public class MessageHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ApiService _apiService;
    private readonly StateManager _stateManager;

    private static readonly HashSet<long> AllowedUserIds = new()
    {
        790102074,
        510963549,
    };

    public MessageHandler(ITelegramBotClient botClient, ApiService apiService, StateManager stateManager)
    {
        _botClient = botClient;
        _apiService = apiService;
        _stateManager = stateManager;
    }

    public async Task HandleUpdateAsync(Update update)
    {
        long userId = 0;

        if (update.Type == UpdateType.Message && update.Message?.From != null)
        {
            userId = update.Message.From.Id;
        }
        else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery?.From != null)
        {
            userId = update.CallbackQuery.From.Id;
        }

        if (userId == 0 || !AllowedUserIds.Contains(userId))
        {
            Console.WriteLine($"Unauthorized access attempt from user {userId}");
            return;
        }

        if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            await HandleMessageAsync(update.Message);
        }
        else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
        {
            await HandleCallbackQueryAsync(update.CallbackQuery);
        }
    }

    private async Task HandleMessageAsync(Message message)
    {
        var chatId = message.Chat.Id;
        var userId = message.From!.Id;
        var text = message.Text!;

        // Видаляємо повідомлення користувача
        try
        {
            await _botClient.DeleteMessageAsync(chatId, message.MessageId);
        }
        catch { }

        if (text == "/start")
        {
            await _stateManager.ClearStateAsync(userId);
            await ShowMainMenuAsync(chatId, userId);
            return;
        }

        var state = await _stateManager.GetStateAsync(userId);
        if (state != null)
        {
            await ProcessFlowInputAsync(chatId, userId, text, state);
        }
        else
        {
            var msg = await _botClient.SendTextMessageAsync(chatId, "Натисніть /start для початку");
            try
            {
                await Task.Delay(3000);
                await _botClient.DeleteMessageAsync(chatId, msg.MessageId);
            }
            catch { }
        }
    }

    private async Task ShowMainMenuAsync(long chatId, long userId)
    {
        var entities = await _apiService.GetAllByUserAsync(userId);
        if (entities == null || entities.Count == 0)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
            InlineKeyboardButton.WithCallbackData("➕ Створити нову сутність", "create_new")
        });
            await _botClient.SendTextMessageAsync(chatId, "У вас немає сутностей", replyMarkup: keyboard);
            return;
        }

        // Сортування: спочатку активні, потім по ID
        var sortedEntities = entities
            .OrderByDescending(e => e.IsActive)  // Активні спочатку (true > false)
            .ThenByDescending(e => e.Id)                    // Потім по ID
            .ToList();

        var buttons = sortedEntities.Select(e =>
            new[] { InlineKeyboardButton.WithCallbackData(
            $"{(e.IsActive ? "🟢" : "🔴")} #{e.Id} - {e.GiftName} ({e.MinPrice}-{e.MaxPrice})",
            $"entity_{e.Id}") }
        ).ToList();

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("➕ Створити нову", "create_new") });

        var inlineKeyboard = new InlineKeyboardMarkup(buttons);
        await _botClient.SendTextMessageAsync(chatId, "Ваші сутності:", replyMarkup: inlineKeyboard);
    }

    private async Task ShowGiftSelectionAsync(long chatId, UserState state, int page = 0)
    {
        var giftsResponse = await _apiService.GetGiftsAsync();
        if (giftsResponse == null || !giftsResponse.Gifts.Any())
        {
            var skipKeyboard = new InlineKeyboardMarkup(new[] {
                InlineKeyboardButton.WithCallbackData("⏭ Пропустити", "skip_field"),
                InlineKeyboardButton.WithCallbackData("❌ Скасувати", "cancel_flow")
            });
            var msg = await _botClient.SendTextMessageAsync(chatId, "⚠️ Не вдалося завантажити список подарунків", replyMarkup: skipKeyboard);
            state.LastBotMessageId = msg.MessageId;
            await _stateManager.SaveStateAsync(state);
            return;
        }

        const int pageSize = 10;
        var totalPages = (int)Math.Ceiling(giftsResponse.Gifts.Count / (double)pageSize);
        page = Math.Max(0, Math.Min(page, totalPages - 1));
        var pageGifts = giftsResponse.Gifts.Skip(page * pageSize).Take(pageSize).ToList();

        var buttons = new List<InlineKeyboardButton[]>();
        for (int i = 0; i < pageGifts.Count; i += 2)
        {
            var row = new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(pageGifts[i].Name, $"gift_{pageGifts[i].Id}_{pageGifts[i].Name}") };
            if (i + 1 < pageGifts.Count) row.Add(InlineKeyboardButton.WithCallbackData(pageGifts[i + 1].Name, $"gift_{pageGifts[i + 1].Id}_{pageGifts[i + 1].Name}"));
            buttons.Add(row.ToArray());
        }

        var navButtons = new List<InlineKeyboardButton>();
        if (page > 0) navButtons.Add(InlineKeyboardButton.WithCallbackData("◀️", $"giftpage_{page - 1}"));
        navButtons.Add(InlineKeyboardButton.WithCallbackData($"📄 {page + 1}/{totalPages}", "current_page"));
        if (page < totalPages - 1) navButtons.Add(InlineKeyboardButton.WithCallbackData("▶️", $"giftpage_{page + 1}"));
        if (navButtons.Any()) buttons.Add(navButtons.ToArray());

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("❌ Скасувати", "cancel_flow") });

        var keyboard = new InlineKeyboardMarkup(buttons);
        var message = await _botClient.SendTextMessageAsync(chatId, $"🎁 Оберіть подарунок (сторінка {page + 1}/{totalPages}):", replyMarkup: keyboard);
        state.LastBotMessageId = message.MessageId;
        await _stateManager.SaveStateAsync(state);
    }

    private async Task ShowModelSelectionAsync(long chatId, UserState state, long giftId, int page = 0)
    {
        var modelsResponse = await _apiService.GetGiftModelsAsync(giftId);
        if (modelsResponse == null || !modelsResponse.GiftModels.Any())
        {
            var skipKeyboard = new InlineKeyboardMarkup(new[] {
                InlineKeyboardButton.WithCallbackData("⏭ Пропустити", "skip_field"),
                InlineKeyboardButton.WithCallbackData("❌ Скасувати", "cancel_flow")
            });
            var msg = await _botClient.SendTextMessageAsync(chatId, "⚠️ Немає доступних моделей", replyMarkup: skipKeyboard);
            state.LastBotMessageId = msg.MessageId;
            await _stateManager.SaveStateAsync(state);
            return;
        }

        const int pageSize = 10;
        var totalPages = (int)Math.Ceiling(modelsResponse.GiftModels.Count / (double)pageSize);
        page = Math.Max(0, Math.Min(page, totalPages - 1));
        var pageModels = modelsResponse.GiftModels.Skip(page * pageSize).Take(pageSize).ToList();

        var buttons = new List<InlineKeyboardButton[]>();
        for (int i = 0; i < pageModels.Count; i += 2)
        {
            var row = new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(pageModels[i].Name, $"model_{pageModels[i].Name}") };
            if (i + 1 < pageModels.Count) row.Add(InlineKeyboardButton.WithCallbackData(pageModels[i + 1].Name, $"model_{pageModels[i + 1].Name}"));
            buttons.Add(row.ToArray());
        }

        var navButtons = new List<InlineKeyboardButton>();
        if (page > 0) navButtons.Add(InlineKeyboardButton.WithCallbackData("◀️", $"modelpage_{page - 1}"));
        navButtons.Add(InlineKeyboardButton.WithCallbackData($"📄 {page + 1}/{totalPages}", "current_page"));
        if (page < totalPages - 1) navButtons.Add(InlineKeyboardButton.WithCallbackData("▶️", $"modelpage_{page + 1}"));
        if (navButtons.Any()) buttons.Add(navButtons.ToArray());

        buttons.Add(new[] {
            InlineKeyboardButton.WithCallbackData("⏭ Пропустити", "skip_field"),
            InlineKeyboardButton.WithCallbackData("❌ Скасувати", "cancel_flow")
        });

        var keyboard = new InlineKeyboardMarkup(buttons);
        var message = await _botClient.SendTextMessageAsync(chatId, $"👤 Оберіть модель (сторінка {page + 1}/{totalPages}):", replyMarkup: keyboard);
        state.LastBotMessageId = message.MessageId;
        await _stateManager.SaveStateAsync(state);
    }

    private async Task ShowSymbolSelectionAsync(long chatId, UserState state, long giftId, int page = 0)
    {
        var symbolsResponse = await _apiService.GetGiftSymbolsAsync(giftId);
        if (symbolsResponse == null || !symbolsResponse.GiftSymbols.Any())
        {
            var skipKeyboard = new InlineKeyboardMarkup(new[] {
                InlineKeyboardButton.WithCallbackData("⏭ Пропустити", "skip_field"),
                InlineKeyboardButton.WithCallbackData("❌ Скасувати", "cancel_flow")
            });
            var msg = await _botClient.SendTextMessageAsync(chatId, "⚠️ Немає доступних символів", replyMarkup: skipKeyboard);
            state.LastBotMessageId = msg.MessageId;
            await _stateManager.SaveStateAsync(state);
            return;
        }

        const int pageSize = 10;
        var totalPages = (int)Math.Ceiling(symbolsResponse.GiftSymbols.Count / (double)pageSize);
        page = Math.Max(0, Math.Min(page, totalPages - 1));
        var pageSymbols = symbolsResponse.GiftSymbols.Skip(page * pageSize).Take(pageSize).ToList();

        var buttons = new List<InlineKeyboardButton[]>();
        for (int i = 0; i < pageSymbols.Count; i += 2)
        {
            var row = new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(pageSymbols[i].Name, $"symbol_{pageSymbols[i].Name}") };
            if (i + 1 < pageSymbols.Count) row.Add(InlineKeyboardButton.WithCallbackData(pageSymbols[i + 1].Name, $"symbol_{pageSymbols[i + 1].Name}"));
            buttons.Add(row.ToArray());
        }

        var navButtons = new List<InlineKeyboardButton>();
        if (page > 0) navButtons.Add(InlineKeyboardButton.WithCallbackData("◀️", $"symbolpage_{page - 1}"));
        navButtons.Add(InlineKeyboardButton.WithCallbackData($"📄 {page + 1}/{totalPages}", "current_page"));
        if (page < totalPages - 1) navButtons.Add(InlineKeyboardButton.WithCallbackData("▶️", $"symbolpage_{page + 1}"));
        if (navButtons.Any()) buttons.Add(navButtons.ToArray());

        buttons.Add(new[] {
            InlineKeyboardButton.WithCallbackData("⏭ Пропустити", "skip_field"),
            InlineKeyboardButton.WithCallbackData("❌ Скасувати", "cancel_flow")
        });

        var keyboard = new InlineKeyboardMarkup(buttons);
        var message = await _botClient.SendTextMessageAsync(chatId, $"🔣 Оберіть символ (сторінка {page + 1}/{totalPages}):", replyMarkup: keyboard);
        state.LastBotMessageId = message.MessageId;
        await _stateManager.SaveStateAsync(state);
    }

    private async Task ShowBackdropSelectionAsync(long chatId, UserState state, long giftId, int page = 0)
    {
        var backdropsResponse = await _apiService.GetGiftBackdropsAsync(giftId);
        if (backdropsResponse == null || !backdropsResponse.GiftBackdrops.Any())
        {
            var skipKeyboard = new InlineKeyboardMarkup(new[] {
                InlineKeyboardButton.WithCallbackData("⏭ Пропустити", "skip_field"),
                InlineKeyboardButton.WithCallbackData("❌ Скасувати", "cancel_flow")
            });
            var msg = await _botClient.SendTextMessageAsync(chatId, "⚠️ Немає доступних фонів", replyMarkup: skipKeyboard);
            state.LastBotMessageId = msg.MessageId;
            await _stateManager.SaveStateAsync(state);
            return;
        }

        const int pageSize = 10;
        var totalPages = (int)Math.Ceiling(backdropsResponse.GiftBackdrops.Count / (double)pageSize);
        page = Math.Max(0, Math.Min(page, totalPages - 1));
        var pageBackdrops = backdropsResponse.GiftBackdrops.Skip(page * pageSize).Take(pageSize).ToList();

        var buttons = new List<InlineKeyboardButton[]>();
        for (int i = 0; i < pageBackdrops.Count; i += 2)
        {
            var row = new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(pageBackdrops[i].Name, $"backdrop_{pageBackdrops[i].Name}") };
            if (i + 1 < pageBackdrops.Count) row.Add(InlineKeyboardButton.WithCallbackData(pageBackdrops[i + 1].Name, $"backdrop_{pageBackdrops[i + 1].Name}"));
            buttons.Add(row.ToArray());
        }

        var navButtons = new List<InlineKeyboardButton>();
        if (page > 0) navButtons.Add(InlineKeyboardButton.WithCallbackData("◀️", $"backdroppage_{page - 1}"));
        navButtons.Add(InlineKeyboardButton.WithCallbackData($"📄 {page + 1}/{totalPages}", "current_page"));
        if (page < totalPages - 1) navButtons.Add(InlineKeyboardButton.WithCallbackData("▶️", $"backdroppage_{page + 1}"));
        if (navButtons.Any()) buttons.Add(navButtons.ToArray());

        buttons.Add(new[] {
            InlineKeyboardButton.WithCallbackData("⏭ Пропустити", "skip_field"),
            InlineKeyboardButton.WithCallbackData("❌ Скасувати", "cancel_flow")
        });

        var keyboard = new InlineKeyboardMarkup(buttons);
        var message = await _botClient.SendTextMessageAsync(chatId, $"🎨 Оберіть фон (сторінка {page + 1}/{totalPages}):", replyMarkup: keyboard);
        state.LastBotMessageId = message.MessageId;
        await _stateManager.SaveStateAsync(state);
    }

    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
    {
        var chatId = callbackQuery.Message!.Chat.Id;
        var userId = callbackQuery.From.Id;
        var data = callbackQuery.Data!;
        var messageId = callbackQuery.Message.MessageId;

        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);

        // Видаляємо попереднє повідомлення
        try { await _botClient.DeleteMessageAsync(chatId, messageId); } catch { }

        var state = await _stateManager.GetStateAsync(userId);

        // ========== НОВІ ОБРОБНИКИ ДЛЯ GIFT/MODEL/SYMBOL/BACKDROP ==========

        // Обробка вибору Gift
        if (data.StartsWith("gift_"))
        {
            var parts = data.Split('_');
            var giftId = long.Parse(parts[1]);
            var giftName = string.Join("_", parts.Skip(2));
            await HandleGiftSelectionAsync(chatId, userId, giftId, giftName, state);
            return;
        }

        // Обробка пагінації Gift
        if (data.StartsWith("giftpage_"))
        {
            var page = int.Parse(data.Split('_')[1]);
            await ShowGiftSelectionAsync(chatId, state!, page);
            return;
        }

        // Обробка вибору Model
        if (data.StartsWith("model_"))
        {
            var modelName = data.Replace("model_", "");
            await HandleModelSelectionAsync(chatId, userId, modelName, state);
            return;
        }

        // Обробка пагінації Model
        if (data.StartsWith("modelpage_"))
        {
            var page = int.Parse(data.Split('_')[1]);
            if (state?.SelectedGiftId.HasValue == true)
                await ShowModelSelectionAsync(chatId, state, state.SelectedGiftId.Value, page);
            return;
        }

        // Обробка вибору Symbol
        if (data.StartsWith("symbol_"))
        {
            var symbolName = data.Replace("symbol_", "");
            await HandleSymbolSelectionAsync(chatId, userId, symbolName, state);
            return;
        }

        // Обробка пагінації Symbol
        if (data.StartsWith("symbolpage_"))
        {
            var page = int.Parse(data.Split('_')[1]);
            if (state?.SelectedGiftId.HasValue == true)
                await ShowSymbolSelectionAsync(chatId, state, state.SelectedGiftId.Value, page);
            return;
        }

        // Обробка вибору Backdrop
        if (data.StartsWith("backdrop_"))
        {
            var backdropName = data.Replace("backdrop_", "");
            await HandleBackdropSelectionAsync(chatId, userId, backdropName, state);
            return;
        }

        // Обробка пагінації Backdrop
        if (data.StartsWith("backdroppage_"))
        {
            var page = int.Parse(data.Split('_')[1]);
            if (state?.SelectedGiftId.HasValue == true)
                await ShowBackdropSelectionAsync(chatId, state, state.SelectedGiftId.Value, page);
            return;
        }

        if (data == "current_page")
        {
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Ви на цій сторінці");
            return;
        }

        // ========== ІСНУЮЧІ ОБРОБНИКИ ==========

        if (data.StartsWith("entity_"))
        {
            var entityId = int.Parse(data.Split('_')[1]);
            await ShowEntityDetailsAsync(chatId, userId, entityId);
        }
        else if (data == "back_to_list")
        {
            await _stateManager.ClearStateAsync(userId);
            await ShowMainMenuAsync(chatId, userId);
        }
        else if (data.StartsWith("update_"))
        {
            var entityId = int.Parse(data.Split('_')[1]);
            await ShowUpdateMenuAsync(chatId, userId, entityId);
        }
        else if (data.StartsWith("edit_"))
        {
            var parts = data.Split('_');
            var field = parts[1];
            var entityId = int.Parse(parts[2]);
            await StartEditFieldAsync(chatId, userId, entityId, field);
        }
        else if (data.StartsWith("delete_"))
        {
            var entityId = int.Parse(data.Split('_')[1]);
            await DeleteEntityAsync(chatId, userId, entityId);
        }
        else if (data == "create_new")
        {
            await StartCreateFlowAsync(chatId, userId);
        }
        else if (data == "cancel_flow")
        {
            await _stateManager.ClearStateAsync(userId);
            await ShowMainMenuAsync(chatId, userId);
        }
        else if (data.StartsWith("finish_edit_"))
        {
            var entityId = int.Parse(data.Split('_')[2]);
            await FinishEditAsync(chatId, userId, entityId);
        }
        else if (data == "skip_field")
        {
            await ProcessFlowInputAsync(chatId, userId, "skip", await _stateManager.GetStateAsync(userId));
        }
        else if (data == "answer_yes")
        {
            await ProcessFlowInputAsync(chatId, userId, "yes", await _stateManager.GetStateAsync(userId));
        }
        else if (data == "answer_no")
        {
            await ProcessFlowInputAsync(chatId, userId, "no", await _stateManager.GetStateAsync(userId));
        }
        else if (data.StartsWith("currency_"))
        {
            var currency = data.Replace("currency_", "");
            await ProcessFlowInputAsync(chatId, userId, currency, await _stateManager.GetStateAsync(userId));
        }
    }

    private async Task HandleGiftSelectionAsync(long chatId, long userId, long giftId, string giftName, UserState? state)
    {
        if (state == null) return;

        var order = JsonSerializer.Deserialize<ResoldGiftOrder>(state.CollectedData!);
        if (order == null) return;

        order.GiftName = giftName; // Зберігаємо NAME

        state.CollectedData = JsonSerializer.Serialize(order);
        state.SelectedGiftId = giftId; // Зберігаємо ID для наступних кроків
        state.CurrentStep++;
        await _stateManager.SaveStateAsync(state);

        await AskNextStepAsync(chatId, state, order);
    }

    private async Task HandleModelSelectionAsync(long chatId, long userId, string modelName, UserState? state)
    {
        if (state == null) return;

        var order = JsonSerializer.Deserialize<ResoldGiftOrder>(state.CollectedData!);
        if (order == null) return;

        order.ModelName = modelName;

        state.CollectedData = JsonSerializer.Serialize(order);
        state.CurrentStep++;
        await _stateManager.SaveStateAsync(state);

        await AskNextStepAsync(chatId, state, order);
    }

    private async Task HandleSymbolSelectionAsync(long chatId, long userId, string symbolName, UserState? state)
    {
        if (state == null) return;

        var order = JsonSerializer.Deserialize<ResoldGiftOrder>(state.CollectedData!);
        if (order == null) return;

        order.SymbolName = symbolName;

        state.CollectedData = JsonSerializer.Serialize(order);
        state.CurrentStep++;
        await _stateManager.SaveStateAsync(state);

        await AskNextStepAsync(chatId, state, order);
    }

    private async Task HandleBackdropSelectionAsync(long chatId, long userId, string backdropName, UserState? state)
    {
        if (state == null) return;

        var order = JsonSerializer.Deserialize<ResoldGiftOrder>(state.CollectedData!);
        if (order == null) return;

        order.BackdropName = backdropName;

        state.CollectedData = JsonSerializer.Serialize(order);
        state.CurrentStep++;
        await _stateManager.SaveStateAsync(state);

        await AskNextStepAsync(chatId, state, order);
    }

    private async Task ShowEntityDetailsAsync(long chatId, long userId, int entityId)
    {
        var entity = await _apiService.GetByIdAsync(entityId);
        if (entity == null)
        {
            await _botClient.SendTextMessageAsync(chatId, "Сутність не знайдено");
            return;
        }

        var message = $"📋 Деталі сутності #{entity.Id}\n\n" +
                      $"👤 Owner ID: {entity.OwnerId}\n" +
                      $"🎁 Gift: {(string.IsNullOrEmpty(entity.GiftName) ? "не встановлено" : entity.GiftName)}\n" +
                      $"👤 Model: {entity.ModelName ?? "не встановлено"}\n" +
                      $"🔣 Symbol: {entity.SymbolName ?? "не встановлено"}\n" +
                      $"🎨 Backdrop: {entity.BackdropName ?? "не встановлено"}\n" +
                      $"💲 Ціна: {entity.MinPrice} - {entity.MaxPrice}\n" +
                      $"📦 Кількість для покупки: {entity.AmountToBuy}\n" +
                      $"🛒 Куплено: {entity.AmountBought}\n" +
                      $"💱 Валюта: {entity.Currency}\n" +
                      $"✅ Активна: {(entity.IsActive ? "Так" : "Ні")}\n" +
                      $"💎 Only TON: {(entity.IsOnlyTonPayment ? "Так" : "Ні")}\n" +
                      $"🔄 Original Details: {(entity.ShouldBuyWithOriginalDetails ? "Так" : "Ні")}";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
        new[] { InlineKeyboardButton.WithCallbackData("🔄 Оновити", $"update_{entity.Id}") },
        new[] { InlineKeyboardButton.WithCallbackData("🗑 Видалити", $"delete_{entity.Id}") },
        new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "back_to_list") }
    });

        await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: keyboard);
    }

    private async Task ShowUpdateMenuAsync(long chatId, long userId, int entityId)
    {
        var entity = await _apiService.GetByIdAsync(entityId);
        if (entity == null)
        {
            await _botClient.SendTextMessageAsync(chatId, "Сутність не знайдено");
            return;
        }

        var state = new UserState
        {
            TelegramUserId = userId,
            CurrentFlow = "select_field_update",
            EntityId = entityId.ToString(),
            CollectedData = JsonSerializer.Serialize(entity)
        };
        await _stateManager.SaveStateAsync(state);

        // ПРИБРАЛИ GIFT з редагування!
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData($"👤 Owner ID: {entity.OwnerId}", $"edit_ownerid_{entityId}") },
            new[] { InlineKeyboardButton.WithCallbackData($"👤 Model: {entity.ModelName ?? "не встановлено"}", $"edit_model_{entityId}") },
            new[] { InlineKeyboardButton.WithCallbackData($"🔣 Symbol: {entity.SymbolName ?? "не встановлено"}", $"edit_symbol_{entityId}") },
            new[] { InlineKeyboardButton.WithCallbackData($"🎨 Backdrop: {entity.BackdropName ?? "не встановлено"}", $"edit_backdrop_{entityId}") },
            new[] { InlineKeyboardButton.WithCallbackData($"💲 Min Price: {entity.MinPrice}", $"edit_minprice_{entityId}") },
            new[] { InlineKeyboardButton.WithCallbackData($"💰 Max Price: {entity.MaxPrice}", $"edit_maxprice_{entityId}") },
            new[] { InlineKeyboardButton.WithCallbackData($"📦 Amount: {entity.AmountToBuy}", $"edit_amount_{entityId}") },
            new[] { InlineKeyboardButton.WithCallbackData($"💱 Currency: {entity.Currency}", $"edit_currency_{entityId}") },
            new[] { InlineKeyboardButton.WithCallbackData($"✅ Active: {(entity.IsActive ? "Так" : "Ні")}", $"edit_active_{entityId}") },
            new[] { InlineKeyboardButton.WithCallbackData($"💎 Only TON: {(entity.IsOnlyTonPayment ? "Так" : "Ні")}", $"edit_onlytonpayment_{entityId}") },
            new[] { InlineKeyboardButton.WithCallbackData($"🔄 Original Details: {(entity.ShouldBuyWithOriginalDetails ? "Так" : "Ні")}", $"edit_originaldetails_{entityId}") },
            new[] { InlineKeyboardButton.WithCallbackData("✅ Завершити оновлення", $"finish_edit_{entityId}") },
            new[] { InlineKeyboardButton.WithCallbackData("❌ Скасувати", "cancel_flow") }
        });

        await _botClient.SendTextMessageAsync(chatId, "Оберіть поле для редагування:", replyMarkup: keyboard);
    }

    private async Task StartEditFieldAsync(long chatId, long userId, int entityId, string field)
    {
        var state = await _stateManager.GetStateAsync(userId);
        if (state == null) return;

        state.CurrentFlow = $"edit_{field}";
        state.CurrentStep = 0;

        var entity = JsonSerializer.Deserialize<ResoldGiftOrder>(state.CollectedData!);

        if (field == "model" && entity?.GiftName != null)
        {
            // Знайти giftId за назвою
            var giftsResponse = await _apiService.GetGiftsAsync();
            var gift = giftsResponse?.Gifts.FirstOrDefault(g => g.Name == entity.GiftName);
            if (gift != null)
            {
                state.SelectedGiftId = gift.Id;
                await _stateManager.SaveStateAsync(state);
                await ShowModelSelectionAsync(chatId, state, gift.Id, 0);
                return;
            }
        }

        if (field == "symbol" && entity?.GiftName != null)
        {
            var giftsResponse = await _apiService.GetGiftsAsync();
            var gift = giftsResponse?.Gifts.FirstOrDefault(g => g.Name == entity.GiftName);
            if (gift != null)
            {
                state.SelectedGiftId = gift.Id;
                await _stateManager.SaveStateAsync(state);
                await ShowSymbolSelectionAsync(chatId, state, gift.Id, 0);
                return;
            }
        }

        if (field == "backdrop" && entity?.GiftName != null)
        {
            var giftsResponse = await _apiService.GetGiftsAsync();
            var gift = giftsResponse?.Gifts.FirstOrDefault(g => g.Name == entity.GiftName);
            if (gift != null)
            {
                state.SelectedGiftId = gift.Id;
                await _stateManager.SaveStateAsync(state);
                await ShowBackdropSelectionAsync(chatId, state, gift.Id, 0);
                return;
            }
        }

        var (prompt, keyboard) = field switch
        {
            "ownerid" => ("👤 Введіть Owner ID:", CreateCancelKeyboard()),
            "model" => ("👤 Введіть model_name:", CreateSkipCancelKeyboard()),
            "symbol" => ("🔣 Введіть symbol_name:", CreateSkipCancelKeyboard()),
            "backdrop" => ("🎨 Введіть backdrop_name:", CreateSkipCancelKeyboard()),
            "minprice" => ("💵 Введіть мінімальну ціну:", CreateCancelKeyboard()),
            "maxprice" => ("💰 Введіть максимальну ціну:", CreateCancelKeyboard()),
            "amount" => ("📦 Введіть кількість:", CreateCancelKeyboard()),
            "currency" => ("💱 Оберіть валюту:", CreateCurrencyKeyboard()),
            "active" => ("✅ Активна?", CreateYesNoKeyboard()),
            "onlytonpayment" => ("💎 Тільки TON оплата?", CreateYesNoKeyboard()),
            "originaldetails" => ("🔄 Купувати з оригінальними деталями?", CreateYesNoKeyboard()),
            _ => ("Введіть нове значення:", CreateCancelKeyboard())
        };

        var message = await _botClient.SendTextMessageAsync(chatId, prompt, replyMarkup: keyboard);

        state.LastBotMessageId = message.MessageId;
        await _stateManager.SaveStateAsync(state);
    }

    private InlineKeyboardMarkup CreateCancelKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
        InlineKeyboardButton.WithCallbackData("❌ Скасувати", "cancel_flow")
    });
    }

    private InlineKeyboardMarkup CreateSkipCancelKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
        new[] { InlineKeyboardButton.WithCallbackData("⏭ Пропустити", "skip_field") },
        new[] { InlineKeyboardButton.WithCallbackData("❌ Скасувати", "cancel_flow") }
    });
    }

    private InlineKeyboardMarkup CreateYesNoKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("✅ Так", "answer_yes"),
            InlineKeyboardButton.WithCallbackData("❌ Ні", "answer_no")
        },
        new[] { InlineKeyboardButton.WithCallbackData("🔙 Скасувати", "cancel_flow") }
    });
    }

    private InlineKeyboardMarkup CreateCurrencyKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("💎 STARS", "currency_STARS"),
            InlineKeyboardButton.WithCallbackData("💰 TON", "currency_TON")
        },
        new[] { InlineKeyboardButton.WithCallbackData("💵 BOTH", "currency_BOTH") },
        new[] { InlineKeyboardButton.WithCallbackData("❌ Скасувати", "cancel_flow") }
    });
    }

    private async Task ProcessFlowInputAsync(long chatId, long userId, string input, UserState state)
    {
        if (state.LastBotMessageId.HasValue)
        {
            try { await _botClient.DeleteMessageAsync(chatId, state.LastBotMessageId.Value); } catch { }
        }

        if (state.CurrentFlow.StartsWith("edit_"))
        {
            await ProcessEditInputAsync(chatId, userId, input, state);
            return;
        }

        var order = JsonSerializer.Deserialize<ResoldGiftOrder>(state.CollectedData!);
        if (order == null) return;

        var steps = new[] {
            "gift_name",
            "model_name",
            "symbol_name",
            "backdrop_name",
            "min_price",
            "max_price",
            "amount_to_buy",
            "currency",
            "is_active",
            "is_only_ton_payment",
            "should_buy_original_details",
            "owner_id"
        };

        var currentField = steps[state.CurrentStep];

        // Заборонити текстовий інпут для gift_name (ТІЛЬКИ кнопки!)
        if (currentField == "gift_name")
        {
            var msg = await _botClient.SendTextMessageAsync(chatId, "⚠️ Будь ласка, оберіть подарунок з кнопок вище");
            try { await Task.Delay(2000); await _botClient.DeleteMessageAsync(chatId, msg.MessageId); } catch { }
            return;
        }

        SetFieldValue(order, currentField, input);
        state.CollectedData = JsonSerializer.Serialize(order);
        state.CurrentStep++;
        await _stateManager.SaveStateAsync(state);

        await AskNextStepAsync(chatId, state, order);
    }

    private async Task ProcessEditInputAsync(long chatId, long userId, string input, UserState state)
    {
        var order = JsonSerializer.Deserialize<ResoldGiftOrder>(state.CollectedData!);
        if (order == null) return;

        var field = state.CurrentFlow.Replace("edit_", "");

        switch (field)
        {
            case "ownerid": order.OwnerId = long.TryParse(input, out var ownerId) ? ownerId : order.OwnerId; break;
            case "model": order.ModelName = input.ToLower() == "skip" ? null : input; break;
            case "symbol": order.SymbolName = input.ToLower() == "skip" ? null : input; break;
            case "backdrop": order.BackdropName = input.ToLower() == "skip" ? null : input; break;
            case "minprice": order.MinPrice = int.TryParse(input, out var min) ? min : order.MinPrice; break;
            case "maxprice": order.MaxPrice = int.TryParse(input, out var max) ? max : order.MaxPrice; break;
            case "amount": order.AmountToBuy = int.TryParse(input, out var amt) ? amt : order.AmountToBuy; break;
            case "currency": order.Currency = input.ToUpper(); break;
            case "active": order.IsActive = input.ToLower() == "yes" || input.ToLower() == "так"; break;
            case "onlytonpayment": order.IsOnlyTonPayment = input.ToLower() == "yes" || input.ToLower() == "так"; break;
            case "originaldetails": order.ShouldBuyWithOriginalDetails = input.ToLower() == "yes" || input.ToLower() == "так"; break;
        }

        state.CollectedData = JsonSerializer.Serialize(order);
        await _stateManager.SaveStateAsync(state);

        var msg = await _botClient.SendTextMessageAsync(chatId, "✅ Поле оновлено!");

        // Видаляємо повідомлення про успіх через 1 секунду
        try
        {
            await Task.Delay(1000);
            await _botClient.DeleteMessageAsync(chatId, msg.MessageId);
        }
        catch { }

        await ShowUpdateMenuFromStateAsync(chatId, userId, order);
    }

    private async Task ShowUpdateMenuFromStateAsync(long chatId, long userId, ResoldGiftOrder entity)
    {
        var entityId = entity.Id;

        var keyboard = new InlineKeyboardMarkup(new[]
        {
        new[] { InlineKeyboardButton.WithCallbackData($"👤 Owner ID: {entity.OwnerId}", $"edit_ownerid_{entityId}") },
        new[] { InlineKeyboardButton.WithCallbackData($"👤 Model: {entity.ModelName ?? "не встановлено"}", $"edit_model_{entityId}") },
        new[] { InlineKeyboardButton.WithCallbackData($"🔣 Symbol: {entity.SymbolName ?? "не встановлено"}", $"edit_symbol_{entityId}") },
        new[] { InlineKeyboardButton.WithCallbackData($"🎨 Backdrop: {entity.BackdropName ?? "не встановлено"}", $"edit_backdrop_{entityId}") },
        new[] { InlineKeyboardButton.WithCallbackData($"💲 Min Price: {entity.MinPrice}", $"edit_minprice_{entityId}") },
        new[] { InlineKeyboardButton.WithCallbackData($"💰 Max Price: {entity.MaxPrice}", $"edit_maxprice_{entityId}") },
        new[] { InlineKeyboardButton.WithCallbackData($"📦 Amount: {entity.AmountToBuy}", $"edit_amount_{entityId}") },
        new[] { InlineKeyboardButton.WithCallbackData($"💱 Currency: {entity.Currency}", $"edit_currency_{entityId}") },
        new[] { InlineKeyboardButton.WithCallbackData($"✅ Active: {(entity.IsActive ? "Так" : "Ні")}", $"edit_active_{entityId}") },
        new[] { InlineKeyboardButton.WithCallbackData($"💎 Only TON: {(entity.IsOnlyTonPayment ? "Так" : "Ні")}", $"edit_onlytonpayment_{entityId}") },
        new[] { InlineKeyboardButton.WithCallbackData($"🔄 Original Details: {(entity.ShouldBuyWithOriginalDetails ? "Так" : "Ні")}", $"edit_originaldetails_{entityId}") },
        new[] { InlineKeyboardButton.WithCallbackData("✅ Завершити оновлення", $"finish_edit_{entityId}") },
        new[] { InlineKeyboardButton.WithCallbackData("❌ Скасувати", "cancel_flow") }
    });

        await _botClient.SendTextMessageAsync(chatId, "Оберіть поле для редагування:", replyMarkup: keyboard);
    }
    private async Task FinishEditAsync(long chatId, long userId, int entityId)
    {
        var state = await _stateManager.GetStateAsync(userId);
        if (state == null) return;

        var order = JsonSerializer.Deserialize<ResoldGiftOrder>(state.CollectedData!);
        if (order == null) return;

        var success = await _apiService.UpdateAsync(entityId, order);
        var statusMessage = await _botClient.SendTextMessageAsync(chatId, success ? "✅ Сутність оновлено!" : "❌ Помилка оновлення");

        await _stateManager.ClearStateAsync(userId);

        // Видаляємо повідомлення про статус через 2 секунди
        try
        {
            await Task.Delay(2000);
            await _botClient.DeleteMessageAsync(chatId, statusMessage.MessageId);
        }
        catch { }

        await ShowMainMenuAsync(chatId, userId);
    }

    private async Task StartCreateFlowAsync(long chatId, long userId)
    {
        var state = new UserState
        {
            TelegramUserId = userId,
            CurrentFlow = "create",
            CurrentStep = 0,
            CollectedData = JsonSerializer.Serialize(new ResoldGiftOrder { UserId = userId })
        };
        await _stateManager.SaveStateAsync(state);
        await AskNextStepAsync(chatId, state, new ResoldGiftOrder { UserId = userId });
    }

    private async Task AskNextStepAsync(long chatId, UserState state, ResoldGiftOrder currentData)
    {
        var steps = new[] {
            "gift_name",
            "model_name",
            "symbol_name",
            "backdrop_name",
            "min_price",
            "max_price",
            "amount_to_buy",
            "currency",
            "is_active",
            "is_only_ton_payment",
            "should_buy_original_details",
            "owner_id"
        };

        if (state.CurrentStep >= steps.Length)
        {
            await FinalizeFlowAsync(chatId, state);
            return;
        }

        var currentField = steps[state.CurrentStep];

        // Для gift/model/symbol/backdrop показуємо кнопки з API
        if (currentField == "gift_name")
        {
            await ShowGiftSelectionAsync(chatId, state, 0);
            return;
        }

        if (currentField == "model_name" && state.SelectedGiftId.HasValue)
        {
            await ShowModelSelectionAsync(chatId, state, state.SelectedGiftId.Value, 0);
            return;
        }

        if (currentField == "symbol_name" && state.SelectedGiftId.HasValue)
        {
            await ShowSymbolSelectionAsync(chatId, state, state.SelectedGiftId.Value, 0);
            return;
        }

        if (currentField == "backdrop_name" && state.SelectedGiftId.HasValue)
        {
            await ShowBackdropSelectionAsync(chatId, state, state.SelectedGiftId.Value, 0);
            return;
        }

        // Для решти полів - стандартний флоу
        var currentValue = GetFieldValue(currentData, currentField);

        var (prompt, keyboard) = currentField switch
        {
            "min_price" => ($"💵 Введіть мінімальну ціну\nПоточне значення: {currentValue}", CreateCancelKeyboard()),
            "max_price" => ($"💰 Введіть максимальну ціну\nПоточне значення: {currentValue}", CreateCancelKeyboard()),
            "amount_to_buy" => ($"📦 Введіть кількість для покупки\nПоточне значення: {currentValue}", CreateCancelKeyboard()),
            "currency" => ($"💱 Оберіть валюту\nПоточне значення: {currentValue}", CreateCurrencyKeyboard()),
            "is_active" => ($"✅ Активна?\nПоточне значення: {currentValue}", CreateYesNoKeyboard()),
            "is_only_ton_payment" => ($"💎 Тільки TON оплата?\nПоточне значення: {currentValue}", CreateYesNoKeyboard()),
            "should_buy_original_details" => ($"🔄 Купувати з оригінальними деталями?\nПоточне значення: {currentValue}", CreateYesNoKeyboard()),
            "owner_id" => ($"👤 Введіть Owner ID\nПоточне значення: {currentValue}", CreateCancelKeyboard()),
            _ => ("Введіть значення", CreateCancelKeyboard())
        };

        var message = await _botClient.SendTextMessageAsync(chatId, prompt, replyMarkup: keyboard);
        state.LastBotMessageId = message.MessageId;
        await _stateManager.SaveStateAsync(state);
    }

    private string GetFieldValue(ResoldGiftOrder order, string field) => field switch
    {
        "gift_name" => order.GiftName ?? "не встановлено",
        "model_name" => order.ModelName ?? "не встановлено",
        "symbol_name" => order.SymbolName ?? "не встановлено",
        "backdrop_name" => order.BackdropName ?? "не встановлено",
        "min_price" => order.MinPrice.ToString(),
        "max_price" => order.MaxPrice.ToString(),
        "amount_to_buy" => order.AmountToBuy.ToString(),
        "currency" => order.Currency ?? "BOTH",
        "is_active" => order.IsActive ? "Так" : "Ні",
        "is_only_ton_payment" => order.IsOnlyTonPayment ? "Так" : "Ні",
        "should_buy_original_details" => order.ShouldBuyWithOriginalDetails ? "Так" : "Ні",
        "owner_id" => order.OwnerId.ToString(),
        _ => "не встановлено"
    };

    private string GetPromptForField(string field, string currentValue)
    {
        return field switch
        {
            "gift_name" => $"🎁 Введіть назву подарунка\nПоточне значення: {currentValue}",
            "model_name" => $"👤 Введіть model_name (або напишіть 'skip' щоб пропустити)\nПоточне значення: {currentValue}",
            "symbol_name" => $"🔣 Введіть symbol_name (або 'skip')\nПоточне значення: {currentValue}",
            "backdrop_name" => $"🎨 Введіть backdrop_name (або 'skip')\nПоточне значення: {currentValue}",
            "min_price" => $"💵 Введіть мінімальну ціну\nПоточне значення: {currentValue}",
            "max_price" => $"💰 Введіть максимальну ціну\nПоточне значення: {currentValue}",
            "amount_to_buy" => $"📦 Введіть кількість для покупки\nПоточне значення: {currentValue}",
            "currency" => $"💱 Введіть валюту (BOTH/TON/STARS)\nПоточне значення: {currentValue}",
            "is_active" => $"✅ Активна? (yes/no)\nПоточне значення: {currentValue}",
            _ => "Введіть значення"
        };
    }

    private void SetFieldValue(ResoldGiftOrder order, string field, string value)
    {
        switch (field)
        {
            case "model_name": order.ModelName = value.ToLower() == "skip" ? null : value; break;
            case "symbol_name": order.SymbolName = value.ToLower() == "skip" ? null : value; break;
            case "backdrop_name": order.BackdropName = value.ToLower() == "skip" ? null : value; break;
            case "min_price": order.MinPrice = int.TryParse(value, out var min) ? min : 1; break;
            case "max_price": order.MaxPrice = int.TryParse(value, out var max) ? max : 100; break;
            case "amount_to_buy": order.AmountToBuy = int.TryParse(value, out var amt) ? amt : 1; break;
            case "currency": order.Currency = value.ToUpper(); break;
            case "is_active": order.IsActive = value.ToLower() == "yes" || value.ToLower() == "так"; break;
            case "is_only_ton_payment": order.IsOnlyTonPayment = value.ToLower() == "yes" || value.ToLower() == "так"; break;
            case "should_buy_original_details": order.ShouldBuyWithOriginalDetails = value.ToLower() == "yes" || value.ToLower() == "так"; break;
            case "owner_id": order.OwnerId = long.TryParse(value, out var ownerId) ? ownerId : 0; break;
        }
    }

    private async Task FinalizeFlowAsync(long chatId, UserState state)
    {
        var order = JsonSerializer.Deserialize<ResoldGiftOrder>(state.CollectedData!);
        if (order == null) return;

        bool success;
        Message statusMessage;

        if (state.CurrentFlow == "create")
        {
            success = await _apiService.CreateAsync(order);
            statusMessage = await _botClient.SendTextMessageAsync(chatId, success ? "✅ Сутність створено!" : "❌ Помилка створення");
        }
        else
        {
            var entityId = int.Parse(state.EntityId!);
            success = await _apiService.UpdateAsync(entityId, order);
            statusMessage = await _botClient.SendTextMessageAsync(chatId, success ? "✅ Сутність оновлено!" : "❌ Помилка оновлення");
        }

        await _stateManager.ClearStateAsync(state.TelegramUserId);

        // Видаляємо повідомлення про статус через 2 секунди
        try
        {
            await Task.Delay(2000);
            await _botClient.DeleteMessageAsync(chatId, statusMessage.MessageId);
        }
        catch { }

        await ShowMainMenuAsync(chatId, state.TelegramUserId);
    }

    private async Task DeleteEntityAsync(long chatId, long userId, int entityId)
    {
        var success = await _apiService.DeleteAsync(entityId);
        var statusMessage = await _botClient.SendTextMessageAsync(chatId, success ? "✅ Сутність видалено!" : "❌ Помилка видалення");

        // Видаляємо повідомлення про статус через 2 секунди
        try
        {
            await Task.Delay(2000);
            await _botClient.DeleteMessageAsync(chatId, statusMessage.MessageId);
        }
        catch { }

        await ShowMainMenuAsync(chatId, userId);
    }
}
