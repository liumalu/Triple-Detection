using System.Collections.Generic;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Entities.Queries;

namespace TripleDetection.Domain.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    IPagedResult<AuditLog> Query(AuditLogQuery query);
    IEnumerable<AuditLog> Export(AuditLogQuery query);
}