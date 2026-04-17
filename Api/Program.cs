using System;
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

// Настройка конвейера обработки HTTP-запросов
if (app.Environment.IsDevelopment())
{
    // OpenAPI документация доступна только в режиме разработки
    app.MapOpenApi();
}
else
{
    // В production используем middleware для обработки исключений
    app.UseMiddleware<ResponseExceptionMiddleware>();
}

// Настройка CORS политики
app.UseCors(builder =>
{
    if (app.Environment.IsDevelopment())
    {
        // В разработке разрешаем все источники для удобства
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    }
    else
    {
        // ВАЖНО: В production используются только разрешённые домены из конфигурации
        builder.WithOrigins(
                   app.Configuration["AllowedOrigins"]?.Split(',')
                   ?? throw new InvalidOperationException("AllowedOrigins не настроен в конфигурации"))
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    }
});

// Мидлвар статистики по запросам.
app.UseMiddleware<StatRequestMiddleware>();

// Подключение FastEndpoints и Swagger
app.UseFastEndpoints().UseSwaggerGen();

app.Run();
