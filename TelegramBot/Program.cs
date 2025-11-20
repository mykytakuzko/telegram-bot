using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using TelegramBotApp.Data;
using TelegramBotApp.Handlers;
using TelegramBotApp.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
        logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddSingleton(sp =>
        {
            var apiConfig = configuration.GetSection("Api");
            return new ApiService(apiConfig["BaseUrl"]!, apiConfig["BearerToken"]!);
        });

        services.AddSingleton<ITelegramBotClient>(sp =>
        {
            var token = configuration.GetSection("TelegramBot")["Token"]!;
            return new TelegramBotClient(token);
        });

        services.AddScoped<StateManager>();
        services.AddScoped<MessageHandler>();
        services.AddSingleton<BotService>();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

var botService = host.Services.GetRequiredService<BotService>();
await botService.StartAsync(CancellationToken.None);

await host.RunAsync();
