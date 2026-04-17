using System.Globalization;

namespace Shared.Const;

public class AppConstants
{
    public const long BotId = 1;

    /// <summary>
    /// https://t.me/{USERNAME}
    /// </summary>
    public const string TelegramLinkFormat = "https://t.me/{0}";
    public const string WhatsAppLinkFormat = "https://wa.me/{0}";
    public const string CallPhoneNumberLinkFormat = "tel:{0}";

    /// <summary>
    /// Прямая ссылка на чат по ИД. Только в боте работает.
    /// </summary>
    public const string PrivateChatLinkFormat = "tg://user?id={0}";

    public class UserRoles
    {
        public const string Admin = "admin";
        public const string User = "user";
    }

    public class Cultures
    {
        public static CultureInfo Ru = new CultureInfo("ru-RU");
    }

    public class Policies
    {
        public const string AdminOnly = "AdminOnly";
    }

    public class TelegramGateway
    {
        public const string VerificationCodeStatusSuccess = "code_valid";
    }

    public const long UndefinedTelegramId = -1;
}
