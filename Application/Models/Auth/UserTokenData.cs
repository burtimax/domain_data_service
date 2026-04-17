namespace Application.Models.Auth;

public class UserTokenData
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public long UserId { get; set; }
    public long UserTelegramId { get; set; }
    public string Role { get; set; }
    public long SessionId { get; set; }
    public string Utm { get; set; }
}
