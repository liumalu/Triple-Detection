namespace TripleDetection.Domain.Entities.Queries
{

public class UserQuery : PagedQuery
{
    public string ExactUsername { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
}
}