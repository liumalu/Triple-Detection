using System;
using System.Collections.Generic;
using System.Linq;
using TripleDetection.Data;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories;
using TripleDetection.Data.Repositories.Sqlite;

namespace TripleDetection.Services.Audit
{
    public interface IAuditLogService
    {
        void Log(int userId, string action, string objectType, int objectId, string details);
        void Log(int userId, string action, string objectType, int objectId, string details, string ipAddress);
        IPagedResult<AuditLog> Query(AuditLogQuery query);
        IEnumerable<AuditLog> Export(AuditLogQuery query);
        IEnumerable<AuditLog> GetByUserId(int userId);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _repository;

        public AuditLogService(IAuditLogRepository repository)
        {
            _repository = repository;
        }

        public void Log(int userId, string action, string objectType, int objectId, string details)
        {
            Log(userId, action, objectType, objectId, details, SessionManager.CurrentIpAddress);
        }

        public void Log(int userId, string action, string objectType, int objectId, string details, string ipAddress)
        {
            try
            {
                var log = new AuditLog
                {
                    UserId = userId,
                    UserName = SessionManager.CurrentUserName,
                    Action = action,
                    ObjectType = objectType,
                    ObjectId = objectId,
                    Details = details,
                    IpAddress = string.IsNullOrEmpty(ipAddress) ? SessionManager.CurrentIpAddress : ipAddress,
                    CreateAt = DateTime.Now
                };
                _repository.Add(log);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AuditLog 保存失败: {ex.Message}");
            }
        }

        public IPagedResult<AuditLog> Query(AuditLogQuery query)
        {
            return _repository.Query(query);
        }

        public IEnumerable<AuditLog> Export(AuditLogQuery query)
        {
            return _repository.Export(query);
        }

        public IEnumerable<AuditLog> GetByUserId(int userId)
        {
            return _repository.Find(x => x.UserId == userId && !x.IsDeleted)
                .OrderByDescending(x => x.CreateAt);
        }
    }
}