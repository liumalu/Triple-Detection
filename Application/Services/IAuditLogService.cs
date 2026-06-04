using System.Collections.Generic;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Entities.Queries;

namespace TripleDetection.Application.Services
{

public interface IAuditLogService
{
    void Log(int userId, string action, string objectType, int objectId, string details);
    void Log(int userId, string action, string objectType, int objectId, string details, string ipAddress);
    IPagedResult<AuditLog> Query(AuditLogQuery query);
    IEnumerable<AuditLog> Export(AuditLogQuery query);
    IEnumerable<AuditLog> GetByUserId(int userId);
}
}