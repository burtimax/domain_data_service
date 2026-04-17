using Application.Models.Auth;
using Infrastructure.Db.App.Entities;
using Shared.Contracts;
using RegisterRequest = Application.Models.Auth.RegisterRequest;

namespace Application.Services.Authentication
{
    public interface IAuthenticationService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
    }
}
