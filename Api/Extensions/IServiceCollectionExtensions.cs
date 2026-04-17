using System;
using Api.BackgroundServices;
using Application.Extensions;
using Application.Services.StatEvent;
using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
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
        // Receiver-only composition root.
        services.AddScoped<IStatEventService, StatEventService>();
        services.AddApplicationServices();
        services.AddHostedService<RabbitMqObservationConsumerService>();
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

        if (string.IsNullOrWhiteSpace(config.RabbitMq.ObservationExchange))
            throw new InvalidOperationException("Configuration.RabbitMq.ObservationExchange is required.");

        if (string.IsNullOrWhiteSpace(config.RabbitMq.ObservationRoutingKey))
            throw new InvalidOperationException("Configuration.RabbitMq.ObservationRoutingKey is required.");

        if (string.IsNullOrWhiteSpace(config.RabbitMq.DlqExchange))
            throw new InvalidOperationException("Configuration.RabbitMq.DlqExchange is required.");

        if (string.IsNullOrWhiteSpace(config.RabbitMq.DlqQueue))
            throw new InvalidOperationException("Configuration.RabbitMq.DlqQueue is required.");

        if (string.IsNullOrWhiteSpace(config.RabbitMq.DlqRoutingKey))
            throw new InvalidOperationException("Configuration.RabbitMq.DlqRoutingKey is required.");
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
