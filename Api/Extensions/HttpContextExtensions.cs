using System;
using System.Linq;
using System.Security.Claims;
using Application.Models.Auth;
using Microsoft.AspNetCore.Http;

namespace Api.Extensions;

public static class HttpContextExtensions
{
    public static UserTokenData TokenData(this HttpContext httpContext)
    {
        UserTokenData userTokenData = new();

        string GetClaim(string claim) => httpContext.User.Claims
            ?.First(x => x.Type.Equals(claim, StringComparison.OrdinalIgnoreCase))?.Value ?? throw new Exception($"NOT FOUND CLAIM IN TOKEN [{claim}]");

        userTokenData.UserTelegramId = long.Parse(GetClaim("UserTelegramId"));
        userTokenData.FirstName = GetClaim("FirstName");
        userTokenData.LastName = GetClaim("LastName");
        userTokenData.MiddleName = GetClaim("MiddleName");
        userTokenData.Role = GetClaim(ClaimTypes.Role);
        userTokenData.UserId = long.Parse(GetClaim(ClaimTypes.NameIdentifier));
        userTokenData.SessionId = long.Parse(GetClaim("SessionId"));
        userTokenData.Utm = GetClaim("Utm");

        return userTokenData;
    }

    public static bool TryGetTokenData(this HttpContext httpContext, out UserTokenData data)
    {
        data = null;

        bool hasAuthHeader = httpContext.Request.Headers.ContainsKey("Authorization");

        if (!hasAuthHeader) return false;

        data = httpContext.TokenData();
        return true;
    }
}
