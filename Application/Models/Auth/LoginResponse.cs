using Infrastructure.Db.App.Entities;

namespace Application.Models.Auth
{
    public class LoginResponse
    {
        public UserEntity User { get; set; }
        public string Token { get; set; }
        public UserEntity Doctor { get; set; }
    }
}
