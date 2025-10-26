using Microsoft.EntityFrameworkCore;
using TelegramBotApp.Data;
using TelegramBotApp.Models;

namespace TelegramBotApp.Services;

public class StateManager
{
    private readonly AppDbContext _context;

    public StateManager(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserState?> GetStateAsync(long telegramUserId)
    {
        return await _context.UserStates.FirstOrDefaultAsync(s => s.TelegramUserId == telegramUserId);
    }

    public async Task SaveStateAsync(UserState state)
    {
        state.UpdatedAt = DateTime.UtcNow;
        var existing = await GetStateAsync(state.TelegramUserId);
        if (existing != null)
        {
            existing.CurrentFlow = state.CurrentFlow;
            existing.CurrentStep = state.CurrentStep;
            existing.EntityId = state.EntityId;
            existing.CollectedData = state.CollectedData;
            existing.UpdatedAt = state.UpdatedAt;
            _context.UserStates.Update(existing);
        }
        else
        {
            _context.UserStates.Add(state);
        }
        await _context.SaveChangesAsync();
    }

    public async Task ClearStateAsync(long telegramUserId)
    {
        var state = await GetStateAsync(telegramUserId);
        if (state != null)
        {
            _context.UserStates.Remove(state);
            await _context.SaveChangesAsync();
        }
    }
}
