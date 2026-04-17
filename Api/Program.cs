using System;
using System.Collections.Generic;
using System.Diagnostics;
using Api.Extensions;
using Api.Middleware;
using Application.Extensions;
using FastEndpoints;
using FastEndpoints.Swagger;
using Infrastructure.Db.App;

using Microsoft.EntityFrameworkCore;


using NSwag;
using NSwag.Generation.Processors.Security;

// Включаем старое поведение timestamp для совместимости с Npgsql
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();

// Настройка конфигурации из различных источников (JSON файлы, переменные окружения)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Настройка FastEndpoints для обработки API-запросов
builder.Services.AddFastEndpoints(o =>
    {
        o.Assemblies = new[]
        {
            typeof(Program).Assembly,
        };
    })
    // Настройка Swagger документации для API
    .SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
        {
            s.Title = "Template API";
            s.Version = "v1";
            s.OperationProcessors.Add(new OperationSecurityScopeProcessor("Bearer"));
        };
    });

builder.Services.AddHttpClient();

// Регистрация сервисов приложения
var services = builder.Services;
var config = services.AddConfigurations(builder.Configuration);
services.AddDatabase(config.Database, builder.Environment);
services.AddServices(builder.Configuration);
services.AddCors();
services.AddMapster();


var app = builder.Build();

// Автоматическое применение миграций базы данных при запуске
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Единый middleware исключений нужен во всех окружениях.
app.UseMiddleware<ResponseExceptionMiddleware>();

// OpenAPI документация доступна только в режиме разработки
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Настройка CORS политики
app.UseCors(builder =>
{
    var allowedOrigins = config.Receiver.AllowedOrigins;
    if (app.Environment.IsDevelopment() && allowedOrigins.Length == 0)
    {
        // В разработке разрешаем все источники для удобства
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    }
    else
    {
        // В non-dev и при явно заданных origin используем whitelist.
        builder.WithOrigins(allowedOrigins)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    }
});

app.Use(async (context, next) =>
{
    var messageId = context.Request.Headers["X-Message-Id"].ToString();
    var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    using var _ = app.Logger.BeginScope(new Dictionary<string, object?>
    {
        ["TraceId"] = traceId,
        ["MessageId"] = string.IsNullOrWhiteSpace(messageId) ? null : messageId,
    });

    await next();
});

// Мидлвар статистики по запросам.
app.UseMiddleware<StatRequestMiddleware>();

// Подключение FastEndpoints и Swagger
app.UseFastEndpoints().UseSwaggerGen();

app.Run();
