using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MultipleBotFramework.Base;
using MultipleBotFramework.Extensions;
using Shared.Configs;
using Shared.Const;

namespace TelegramBot.BotHandlers;

public class BaseAppBotHandler : BaseBotHandler
{
    public AppDbContext Db { get; set; }

    public BaseAppBotHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        this.Db = serviceProvider.GetRequiredService<AppDbContext>();
    }

    protected AppConfiguration AppConfig => ServiceProvider.GetRequiredService<AppConfiguration>();
    
    private UserEntity? _appUser = null;
    public async ValueTask<UserEntity?> AppUser()
    {
        if (_appUser != null) return _appUser;
        _appUser = await Db.Users.FirstOrDefaultAsync(u => u.TelegramId == User!.TelegramId);
        return _appUser;
    }
    
    
    public T Handler<T>() where T : IBaseBotHandler
    {
        return this.GetHandlerInstance<T>();
    }
}