using MultipleBotFramework.Services.Interfaces;

namespace TelegramBot.Services.Notification;

public class NotificationService : INotificationService
{
    private readonly IBotNotificationService _botNotificationService;
    
    public NotificationService(IBotNotificationService botNotificationService)
    {
        _botNotificationService = botNotificationService;
    }
    
    public async Task SendTextNotification(long botId, long chatId, string text, string? type = null, string? key = null)
    {
        await _botNotificationService.AddNotification(botId:botId, chatId: chatId, text:text, type:type, key:key);
    }
}