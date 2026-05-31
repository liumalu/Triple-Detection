using TripleDetection.Domain.Entities;

namespace TripleDetection.Domain.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    IPagedResult<AuditLog> Query(AuditLogQuery query);
    IEnumerable<AuditLog> Export(AuditLogQuery query);
}