

using System.ComponentModel;
using Shared.Models;

namespace Application.Models.User;

public class GetUserRequest : Pagination, IOrdered
{
    public List<long>? Ids { get; set; }
    public List<long>? TelegramIds { get; set; }
    public List<string>? Roles { get; set; }
    public string? Search { get; set; }
    public string? Order { get; set; }
}