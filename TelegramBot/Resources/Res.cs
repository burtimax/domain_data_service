using MultipleBotFramework.Utils.Keyboard;
using Shared.Const;
using Shared.Extensions;
using Telegram.BotAPI.AvailableTypes;
using TelegramBot.Resources.Localization;

namespace TelegramBot.Resources;

public class Res
{
    
    
    public static InlineKeyboardBuilder GetDoctorProfileTmaInlineKeyboard(string appUrl)
    {
        InlineKeyboardBuilder kb = new();
        kb.NewRow().Add(new InlineKeyboardButton(ButtonDoctorProfile) { WebApp = new WebAppInfo(appUrl) });
        
        return kb;
    }
    
    public static InlineKeyboardBuilder GetMainInlineKeyboard(long doctorTelegramChatId, string appUrl, string supportChatLink)
    {
        InlineKeyboardBuilder kb = new();
        kb.NewRow().Add(new InlineKeyboardButton(ButtonDoctorChat)
                { Url = AppConstants.PrivateChatLinkFormat.F(doctorTelegramChatId) })
            .NewRow().Add(new InlineKeyboardButton(ButtonOpenApp) { WebApp = new WebAppInfo(appUrl) })
            .NewRow().Add(new InlineKeyboardButton(ButtonSupport) { Url = supportChatLink });
        
       return kb;
    }
}