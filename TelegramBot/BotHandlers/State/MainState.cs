using MultipleBotFramework.Dispatcher.HandlerResolvers;
using MultipleBotFramework.Utils.Keyboard;
using Shared.Configs;
using Shared.Const;
using Shared.Extensions;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;
using TelegramBot.Resources;

namespace TelegramBot.BotHandlers.State;

[BotHandler(stateName:Name, version: 2.0f)]
public class MainState : BaseAppBotHandler
{
    public const string Name = "MainState";
    
    public MainState(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        NotExpectedMessage = "Не понял";
    }

    public override async Task HandleBotRequest(Update update)
    {
        await Answer("Привет");
    }


}