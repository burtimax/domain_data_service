using MultipleBotFramework.Dispatcher.HandlerResolvers;
using MultipleBotFramework.Utils.Keyboard;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;
using TelegramBot.BotHandlers.State;

namespace TelegramBot.BotHandlers.Command;

[BotHandler(command:"/dev", version: 2.0f)]
public class DevCommand : BaseAppBotHandler
{
    public DevCommand(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        NotExpectedMessage = "Пришли ссылку на приложение";
    }

    public override async Task HandleBotRequest(Update update)
    {
        var message = update.Message;
        
        if (message.Entities == null || message.Entities.Count() < 1 ||
            message.Entities.Any(e => e.Type == "mention" || e.Type == "url") == false)
        {
            await Answer(NotExpectedMessage);
            return;
        }
    
        MessageEntity urlOrMention = message.Entities.First(e => e.Type == "url");
        string resLink = urlOrMention.Url ?? message.Text!.Substring(urlOrMention.Offset, urlOrMention.Length);
    
        // Добавить в очередь на модерацию.
        InlineKeyboardBuilder kb = new();
        kb.NewRow()
            .Add(new InlineKeyboardButton("ТЫК")
            {
                WebApp = new WebAppInfo(resLink)
                {
                    
                }
            });
        await Answer("Держи кнопку на приложение", replyMarkup: kb.Build());
    }
}