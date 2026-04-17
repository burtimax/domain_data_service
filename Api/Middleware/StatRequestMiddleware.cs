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
        string path = context.Request.Path;
        await statEventService.CreateStatEventAsync(path);

        await next.Invoke(context);

    }
}
