using TripleDetection.Domain.Entities;

namespace TripleDetection.Application.Services;

public interface ITaskService
{
    IEnumerable<ProdTask> GetAll();
    IEnumerable<ProdTask> GetByStatus(TaskStatus status);
    ProdTask GetById(int id);
    void Create(ProdTask task, string createBy, int currentUserId);
    void Update(ProdTask task, string updateBy, int currentUserId);
    void Approve(int id, string reviewedBy, int currentUserId);
    void UpdateStatus(int id, TaskStatus status, string updateBy, int currentUserId);
    void Delete(int id, string updateBy, int currentUserId);
    IPagedResult<ProdTask> Query(PagedQuery query);
    IPagedResult<ProdTask> Query(TaskQuery query);
}