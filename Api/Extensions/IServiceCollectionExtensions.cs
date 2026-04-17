using System;
using Infrastructure.Db.App;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Configs;

namespace Api.Extensions;

public static class IServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Receiver-only composition root: legacy bot/sms сервисы не регистрируются.
        _ = configuration;
    }

    public static AppConfiguration AddConfigurations(this IServiceCollection services, IConfiguration configuration)
    {
        AppConfiguration? config = configuration.Get<AppConfiguration>();
        if (config is null) throw new NullReferenceException(nameof(config));

        ValidateConfiguration(config);
        services.AddSingleton(config);
        return config;
    }

    private static void ValidateConfiguration(AppConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.Database.AppDbConnection))
            throw new InvalidOperationException("Configuration.Database.AppDbConnection is required.");

        if (config.Receiver.MaxAcceptedClockSkewMinutes <= 0)
            throw new InvalidOperationException("Configuration.Receiver.MaxAcceptedClockSkewMinutes must be greater than 0.");

        if (string.IsNullOrWhiteSpace(config.RabbitMq.Host))
            throw new InvalidOperationException("Configuration.RabbitMq.Host is required.");

        if (config.RabbitMq.Port <= 0)
            throw new InvalidOperationException("Configuration.RabbitMq.Port must be greater than 0.");

        if (string.IsNullOrWhiteSpace(config.RabbitMq.ObservationQueue))
            throw new InvalidOperationException("Configuration.RabbitMq.ObservationQueue is required.");
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
