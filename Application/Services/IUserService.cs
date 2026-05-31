using TripleDetection.Domain.Entities;

namespace TripleDetection.Application.Services;

public interface IUserService
{
    User Authenticate(string username, string password);
    IEnumerable<User> GetAll();
    User GetByUsername(string username);
    User GetById(int id);
    void Create(User user, string createBy, int currentUserId);
    void Update(User user, string updateBy, int currentUserId);
    void Delete(string username, string updateBy, int currentUserId);
    void Enable(string username, string updateBy, int currentUserId);
    void Disable(string username, string updateBy, int currentUserId);
    void Lock(string username, string updateBy, int currentUserId);
    void Unlock(string username, string updateBy, int currentUserId);
    IPagedResult<User> Query(UserQuery query);
}