using TripleDetection.Data.Repositories;

namespace TripleDetection.Data.Entities
{
    public class UserQuery : PagedQuery
    {
        public string ExactUsername { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string StatusText { get; set; }
    }
}
