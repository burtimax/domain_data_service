namespace TelegramBot.Services.Notification;

public interface INotificationService
{
    public Task SendTextNotification(long botId, long chatId, string text, string type = null, string key = null);
}