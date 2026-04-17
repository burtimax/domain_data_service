// using FastEndpoints;
// //using FastEndpoints.Swagger;
// using MultipleBotFramework.Extensions;
//
// var builder = WebApplication.CreateBuilder(args);
// var services = builder.Services;
//
// // Add services to the container.
// // builder.Services.AddFastEndpoints(o =>
// //     {
// //         o.Assemblies = new[]
// //         {
// //             typeof(GetBotsEndpoint).Assembly,
// //             typeof(Program).Assembly,
// //         };
// //     })
// //     .SwaggerDocument(o =>
// //     {
// //         o.DocumentSettings = s =>
// //         {
// //             s.Title = "Multiple Bots Template Project API";
// //             s.Version = "v1";
// //         };
// //     });
//
// services.AddBot(builder.Configuration); // Подключаем бота
// services.AddHttpContextAccessor();
// services.AddCors();
//
// var app = builder.Build();
// app.UseBot();
// app.UseHttpsRedirection();
// //app.UseFastEndpoints().UseSwaggerGen(); TODO поправить
//
// app.UseCors(builder =>
// {
//     builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
// });
//
// app.Run();