

using Api.Endpoints.App.SaveStatEvent;
using Api.Extensions;
using Application.Services.StatEvent;

namespace Api.Middleware;

public class StatRequestMiddleware
{
    private readonly RequestDelegate next;

    public StatRequestMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context, IStatEventService statEventService)
    {

        if (context.TryGetTokenData(out var tokenData))
        {
            long userId = tokenData.UserId;
            long sessionId = tokenData.SessionId;
            string? utm = tokenData.Utm;
            string data = context.Request.Path;
            await statEventService.CreateStatEventAsync(userId: userId, sessionId: sessionId, utm: utm, data);
        }

        await next.Invoke(context);

    }
}
