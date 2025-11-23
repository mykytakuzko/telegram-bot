using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using TelegramBotApp.Models;

namespace TelegramBotApp.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public ApiService(string baseUrl, string bearerToken)
    {
        _baseUrl = baseUrl;

        // Створюємо HttpClientHandler з вимкненою перевіркою SSL
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        };

        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
    }

    public async Task<List<ResoldGiftOrder>?> GetAllByUserAsync(long userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/marketplace/resold-gift-order/user/{userId}");
            if (!response.IsSuccessStatusCode) return null;
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ResoldGiftOrder>>(content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetAllByUserAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<ResoldGiftOrder?> GetByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/marketplace/resold-gift-order/{id}");
            if (!response.IsSuccessStatusCode) return null;
            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"API Response: {content}"); // ЛОГУВАННЯ

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<ResoldGiftOrder>(content, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetByIdAsync: {ex.Message}");
            return null;
        }
    }
    public async Task<List<ResoldGiftOrder>?> GetAllActiveAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/marketplace/resold-gift-order/active");
            if (!response.IsSuccessStatusCode) return null;
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ResoldGiftOrder>>(content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetAllActiveAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<List<ResoldGiftOrder>?> GetActiveByUserAsync(long userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/marketplace/resold-gift-order/user/{userId}/active");
            if (!response.IsSuccessStatusCode) return null;
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ResoldGiftOrder>>(content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetActiveByUserAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<(bool success, string? error)> CreateAsync(ResoldGiftOrder order)
    {
        try
        {
            var json = JsonSerializer.Serialize(order);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/marketplace/resold-gift-order", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error creating order: {response.StatusCode} - {errorContent}");
                return (false, $"Помилка створення ({response.StatusCode}): {errorContent}");
            }
            
            return (true, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CreateAsync: {ex.Message}");
            return (false, $"Помилка створення: {ex.Message}");
        }
    }

    public async Task<(bool success, string? error)> UpdateAsync(int id, ResoldGiftOrder order)
    {
        try
        {
            var json = JsonSerializer.Serialize(order);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}/api/marketplace/resold-gift-order/{id}", content);
            Console.WriteLine($"Response for update {response}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error updating order {id}: {response.StatusCode} - {errorContent}");
                return (false, $"Помилка оновлення ({response.StatusCode}): {errorContent}");
            }
            
            return (true, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in UpdateAsync: {ex.Message}");
            return (false, $"Помилка оновлення: {ex.Message}");
        }
    }

    public async Task<(bool success, string? error)> DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/marketplace/resold-gift-order/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error deleting order {id}: {response.StatusCode} - {errorContent}");
                return (false, $"Помилка видалення ({response.StatusCode}): {errorContent}");
            }
            
            return (true, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DeleteAsync: {ex.Message}");
            return (false, $"Помилка видалення: {ex.Message}");
        }
    }

    public async Task<GiftsResponse?> GetGiftsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/marketplace/gifts");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error fetching gifts: {response.StatusCode}");
                return null;
            }
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<GiftsResponse>(content, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetGiftsAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<GiftModelsResponse?> GetGiftModelsAsync(long giftId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/marketplace/gift-models/{giftId}");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error fetching gift models: {response.StatusCode}");
                return null;
            }
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<GiftModelsResponse>(content, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetGiftModelsAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<GiftSymbolsResponse?> GetGiftSymbolsAsync(long giftId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/marketplace/gift-symbols/{giftId}");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error fetching gift symbols: {response.StatusCode}");
                return null;
            }
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<GiftSymbolsResponse>(content, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetGiftSymbolsAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<GiftBackdropsResponse?> GetGiftBackdropsAsync(long giftId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/marketplace/gift-backdrops/{giftId}");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error fetching gift backdrops: {response.StatusCode}");
                return null;
            }
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<GiftBackdropsResponse>(content, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetGiftBackdropsAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<(bool success, string? error)> CreateMonitoringConfigAsync(MonitoringConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config);
            Console.WriteLine($"Sending monitoring config: {json}");
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/marketplace/monitoring/config", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error creating monitoring config: {response.StatusCode}");
                Console.WriteLine($"Response body: {errorContent}");
                return (false, $"Помилка створення ({response.StatusCode}): {errorContent}");
            }
            
            return (true, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CreateMonitoringConfigAsync: {ex.Message}");
            return (false, $"Помилка створення: {ex.Message}");
        }
    }

    public async Task<MonitoringConfigsResponse?> GetMonitoringConfigsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/marketplace/monitoring/configs");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error fetching monitoring configs: {response.StatusCode}");
                return null;
            }
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<MonitoringConfigsResponse>(content, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetMonitoringConfigsAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<MonitoringConfig?> GetMonitoringConfigByIdAsync(int id)
    {
        try
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Fetching monitoring config ID: {id}");
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/marketplace/monitoring/configs/{id}");
            
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] API Response Status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error fetching monitoring config {id}: {response.StatusCode}");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error response body: {errorContent}");
                return null;
            }
            
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] API Response content: {content}");
            
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            // API returns the config directly, not wrapped in a "data" field
            var config = JsonSerializer.Deserialize<MonitoringConfig>(content, options);
            
            if (config != null)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Successfully deserialized config {id}");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Deserialization returned null for config {id}");
            }
            
            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Exception in GetMonitoringConfigByIdAsync: {ex.Message}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    public async Task<(bool success, string? error)> UpdateMonitoringConfigAsync(int id, MonitoringConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config);
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Updating monitoring config {id}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Request body: {json}");
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}/api/marketplace/monitoring/configs/{id}", content);
            
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Update response status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Update failed: {errorContent}");
                return (false, $"Помилка оновлення ({response.StatusCode}): {errorContent}");
            }
            
            return (true, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error updating monitoring config: {ex.Message}");
            return (false, $"Помилка оновлення: {ex.Message}");
        }
    }

    public async Task<(bool success, string? error)> DeleteMonitoringConfigAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/marketplace/monitoring/configs/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error deleting monitoring config {id}: {response.StatusCode} - {errorContent}");
                return (false, $"Помилка видалення ({response.StatusCode}): {errorContent}");
            }
            
            return (true, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DeleteMonitoringConfigAsync: {ex.Message}");
            return (false, $"Помилка видалення: {ex.Message}");
        }
    }
}
