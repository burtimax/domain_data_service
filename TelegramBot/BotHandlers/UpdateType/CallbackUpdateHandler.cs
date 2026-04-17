using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MultipleBotFramework.Base;
using MultipleBotFramework.Dispatcher.HandlerResolvers;
using MultipleBotFramework.Extensions;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.GettingUpdates;

namespace TelegramBot.BotHandlers.UpdateType;

[BotHandler(updateTypes: new []{ MultipleBotFramework.Enums.UpdateType.CallbackQuery })]
public class CallbackUpdateHandler : BaseBotHandler
{
    
    public CallbackUpdateHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public override async Task HandleBotRequest(Update update)
    {
        if (update.Type() != MultipleBotFramework.Enums.UpdateType.CallbackQuery) return;

        
        var callback = update.CallbackQuery;

        await BotClient.AnswerCallbackQueryAsync(callback.Id);
        await Answer("Зачем жмешь на кнопки)");
    }
}