using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MultipleBotFramework.Extensions;
using TelegramBot.Services.Notification;

namespace TelegramBot;

public static class TelegramBotModule
{
    public static void AddTelegramBotModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBot(configuration); // Подключаем бота

        services.AddScoped<INotificationService, NotificationService>();
    }
}