using System.Text.Json;

namespace Application.Models.User;

public class UpdateUserRequest
{
    public long Id { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? HiddenDescription { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? BirthDate { get; set; }
    public int? Gender { get; set; }
    // Любая структура JSON
    public JsonDocument? Additional { get; set; }
}