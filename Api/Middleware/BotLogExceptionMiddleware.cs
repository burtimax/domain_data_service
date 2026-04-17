using System.Text;
using Api.Extensions;
using Application.Extensions;
using Application.Utils;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shared.Const;
using Shared.Contracts;
namespace Api.Middleware;

public class BotLogExceptionMiddleware
{
    private readonly RequestDelegate next;

    public BotLogExceptionMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next.Invoke(context);
        }
        catch (Exception e)
        {
            await context.Response.SendAsync(Result.Failure(e.Message), StatusCodes.Status500InternalServerError);
        }
    }

    private Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    public async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        context.Request.EnableBuffering(); // позволяет повторно читать Body

        context.Request.Body.Position = 0;

        string jsonContent;
        using (var reader = new StreamReader(context.Request.Body, System.Text.Encoding.UTF8))
        {
            jsonContent = await reader.ReadToEndAsync();
        }

        context.Request.Body.Position = 0; // сбросить позицию, чтобы Body можно было прочитать ещё раз
        return jsonContent;
    }
}
