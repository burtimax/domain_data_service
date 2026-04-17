using System;
using Application.Extensions;
using Application.Services.SMSGateway;
using Application.Services.Test;
using Infrastructure.Db.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Configs;

namespace Api.Extensions;

public static class IServiceCollectionExtensions
{
    // /// <summary>
    // /// Добавить маппер в DI.
    // /// </summary>
    // /// <param name="services"></param>
    // public static void AddMapster(this IServiceCollection services)
    // {
    //     var config = TypeAdapterConfig.GlobalSettings;
    //     config.Scan(
    //         Assembly.GetExecutingAssembly());
    //     config.RequireExplicitMapping = false;
    //     config.RequireDestinationMemberSource = false;
    //
    //     config.When((srcType, destType, _) => true)
    //         .IgnoreNullValues(true);
    //
    //     config
    //         .When((srcType, destType, _) => srcType == typeof(IBaseBotEntityWithoutIdentity) == false && destType == typeof(IBaseBotEntityWithoutIdentity))
    //         .Ignore("Id",
    //             nameof(IBaseBotEntityWithoutIdentity.CreatedAt),
    //             nameof(IBaseBotEntityWithoutIdentity.UpdatedAt),
    //             nameof(IBaseBotEntityWithoutIdentity.DeletedAt));
    //
    //     var mapperConfig = new Mapper(config);
    //     services.AddSingleton<IMapper>(mapperConfig);
    // }

    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITestService, TestService>();
        services.AddScoped<ISMSGatewayService, SMSGatewayService>();
    }

    public static AppConfiguration AddConfigurations(this IServiceCollection services, IConfiguration configuration)
    {
        AppConfiguration config = configuration.Get<AppConfiguration>();
        if(config == null) throw new NullReferenceException(nameof(config));
        services.AddSingleton(config);
        return config;
    }

    /// <summary>
    /// Регистрирует контекст базы данных в DI контейнере
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <param name="config">Конфигурация подключения к БД</param>
    /// <param name="environment">Информация об окружении приложения</param>
    public static void AddDatabase(
        this IServiceCollection services,
        DatabaseAppConfiguration config,
        IHostEnvironment environment)
    {
        services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(config.AppDbConnection);

                // ВАЖНО: EnableSensitiveDataLogging включается только в режиме разработки
                // В production это может привести к утечке конфиденциальных данных в логах
                if (environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            }
        );
    }
}
