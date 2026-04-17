using MultipleBotFramework.Dispatcher.HandlerResolvers;
using MultipleBotFramework.Extensions;
using MultipleBotFramework.Utils.Keyboard;
using Shared.Const;
using Telegram.BotAPI.GettingUpdates;
using TelegramBot.BotHandlers.State;
using TelegramBot.Resources;

namespace TelegramBot.BotHandlers.Command;

[BotHandler(command:"/start", version: 2.0f)]
public class StartCommand : BaseAppBotHandler
{
    public StartCommand(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public override async Task HandleBotRequest(Update update)
    {
        string referralCode = update.Message!.Text.Split(" ").Last();
        
        await Handler<MainState>().HandleBotRequest(update);
    }
}