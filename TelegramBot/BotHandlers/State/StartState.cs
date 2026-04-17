using MultipleBotFramework.Dispatcher.HandlerResolvers;
using MultipleBotFramework.Extensions;
using MultipleBotFramework.Utils.Keyboard;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;

namespace TelegramBot.BotHandlers.State;

[BotHandler(stateName:Name, version: 2.0f)]
public class StartHandler : BaseAppBotHandler
{
    public const string Name = "StartState";
    
    public StartHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        NotExpectedMessage = "Пришли ссылку на приложение";
    }

    public override async Task HandleBotRequest(Update update)
    {
        await Handler<MainState>().HandleBotRequest(update);
        await ChangeState(MainState.Name);
    }
}