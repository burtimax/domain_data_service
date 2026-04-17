namespace Application.Models.Auth
{
    sealed public class RegisterRequest
    {
        public long TelegramId { get; set; }
        public long BotId { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public DateOnly? BirthDate { get; set; }
        public int Gender { get; set; }
        public string? HiddenDescription { get; set; }
        public string Role { get; set; }
    }
}
