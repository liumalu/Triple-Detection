using System;
using System.Collections.Generic;
using System.Linq;
using TripleDetection.Data;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories;

namespace TripleDetection.Services
{
    public interface IProductService
    {
        IEnumerable<Product> GetAll();
        Product GetById(int id);
        void Create(Product product, string createBy, int currentUserId);
        void Update(Product product, string updateBy, int currentUserId);
        void Delete(int id, string updateBy, int currentUserId);
        IPagedResult<Product> Query(PagedQuery query);
        IPagedResult<Product> Query(ProductQuery query);
    }

    public class ProductService : IProductService
    {
        private readonly IRepository<Product> _repository;
        private readonly IAuditLogService _auditLog;

        public ProductService() : this(new InMemoryRepository<Product>(), null)
        {
        }

        public ProductService(IRepository<Product> repository, IAuditLogService auditLogService)
        {
            _repository = repository;
            _auditLog = auditLogService;
        }

        public IEnumerable<Product> GetAll()
        {
            return _repository.GetAll();
        }

        public Product GetById(int id)
        {
            return _repository.GetById(id);
        }

        public void Create(Product product, string createBy, int currentUserId)
        {
            product.CreateBy = createBy;
            product.UpdateBy = createBy;
            product.CreateAt = DateTime.Now;
            product.UpdateAt = DateTime.Now;
            product.IsDeleted = false;
            _repository.Add(product);
            _auditLog?.Log(currentUserId, "创建", "Product", product.Id, $"创建产品: {product.Name}");
        }

        public void Update(Product product, string updateBy, int currentUserId)
        {
            product.UpdateBy = updateBy;
            product.UpdateAt = DateTime.Now;
            _repository.Update(product);
            _auditLog?.Log(currentUserId, "修改", "Product", product.Id, $"修改产品: {product.Name}");
        }

        public void Delete(int id, string updateBy, int currentUserId)
        {
            var product = _repository.GetById(id);
            if (product != null)
            {
                product.IsDeleted = true;
                product.UpdateBy = updateBy;
                product.UpdateAt = DateTime.Now;
                _repository.Update(product);
                _auditLog?.Log(currentUserId, "删除", "Product", id, $"删除产品ID: {id}");
            }
        }

        public IPagedResult<Product> Query(PagedQuery query)
        {
            if (_repository is InMemoryRepository<Product> inMemoryRepo)
            {
                return inMemoryRepo.Query(query);
            }
            return new PagedResult<Product>(new List<Product>(), 0, query.PageIndex, query.PageSize);
        }

        public IPagedResult<Product> Query(ProductQuery query)
        {
            if (_repository is InMemoryRepository<Product> inMemoryRepo)
            {
                return inMemoryRepo.Query(query);
            }
            return new PagedResult<Product>(new List<Product>(), 0, query.PageIndex, query.PageSize);
        }
    }

    public interface ITaskService
    {
        IEnumerable<Data.Entities.Task> GetAll();
        IEnumerable<Data.Entities.Task> GetByStatus(TaskStatus status);
        Data.Entities.Task GetById(int id);
        void Create(Data.Entities.Task task, string createBy, int currentUserId);
        void Update(Data.Entities.Task task, string updateBy, int currentUserId);
        void Approve(int id, string reviewedBy, int currentUserId);
        void UpdateStatus(int id, TaskStatus status, string updateBy, int currentUserId);
        void Delete(int id, string updateBy, int currentUserId);
        IPagedResult<Data.Entities.Task> Query(PagedQuery query);
        IPagedResult<Data.Entities.Task> Query(TaskQuery query);
    }

    public class TaskService : ITaskService
    {
        private readonly IRepository<Data.Entities.Task> _repository;
        private readonly IAuditLogService _auditLog;

        public TaskService() : this(new InMemoryRepository<Data.Entities.Task>(), null)
        {
        }

        public TaskService(IRepository<Data.Entities.Task> repository, IAuditLogService auditLogService)
        {
            _repository = repository;
            _auditLog = auditLogService;
        }

        public IEnumerable<Data.Entities.Task> GetAll()
        {
            return _repository.GetAll();
        }

        public IEnumerable<Data.Entities.Task> GetByStatus(TaskStatus status)
        {
            return _repository.Find(x => x.Status == status);
        }

        public Data.Entities.Task GetById(int id)
        {
            return _repository.GetById(id);
        }

        public void Create(Data.Entities.Task task, string createBy, int currentUserId)
        {
            task.CreateBy = createBy;
            task.UpdateBy = createBy;
            task.CreateAt = DateTime.Now;
            task.UpdateAt = DateTime.Now;
            task.IsDeleted = false;
            task.Status = TaskStatus.Pending;
            _repository.Add(task);
            _auditLog?.Log(currentUserId, "创建", "Task", task.Id, $"创建任务: {task.Name}");
        }

        public void Update(Data.Entities.Task task, string updateBy, int currentUserId)
        {
            task.UpdateBy = updateBy;
            task.UpdateAt = DateTime.Now;
            _repository.Update(task);
            _auditLog?.Log(currentUserId, "修改", "Task", task.Id, $"修改任务: {task.Name}");
        }

        public void Approve(int id, string reviewedBy, int currentUserId)
        {
            var task = _repository.GetById(id);
            if (task != null && task.Status == TaskStatus.Pending)
            {
                task.Status = TaskStatus.Approved;
                task.ReviewedBy = reviewedBy;
                task.ReviewedAt = DateTime.Now;
                task.UpdateBy = reviewedBy;
                task.UpdateAt = DateTime.Now;
                _repository.Update(task);
                _auditLog?.Log(currentUserId, "审批", "Task", id, $"审批任务: {task.Name}");
            }
        }

        public void UpdateStatus(int id, TaskStatus status, string updateBy, int currentUserId)
        {
            var task = _repository.GetById(id);
            if (task != null)
            {
                task.Status = status;
                task.UpdateBy = updateBy;
                task.UpdateAt = DateTime.Now;
                _repository.Update(task);
                _auditLog?.Log(currentUserId, "状态变更", "Task", id, $"任务状态变更为: {status}");
            }
        }

        public void Delete(int id, string updateBy, int currentUserId)
        {
            var task = _repository.GetById(id);
            if (task != null)
            {
                task.IsDeleted = true;
                task.UpdateBy = updateBy;
                task.UpdateAt = DateTime.Now;
                _repository.Update(task);
                _auditLog?.Log(currentUserId, "删除", "Task", id, $"删除任务ID: {id}");
            }
        }

        public IPagedResult<Data.Entities.Task> Query(PagedQuery query)
        {
            if (_repository is InMemoryRepository<Data.Entities.Task> inMemoryRepo)
            {
                return inMemoryRepo.Query(query);
            }
            return new PagedResult<Data.Entities.Task>(new List<Data.Entities.Task>(), 0, query.PageIndex, query.PageSize);
        }

        public IPagedResult<Data.Entities.Task> Query(TaskQuery query)
        {
            if (_repository is InMemoryRepository<Data.Entities.Task> inMemoryRepo)
            {
                return inMemoryRepo.Query(query);
            }
            return new PagedResult<Data.Entities.Task>(new List<Data.Entities.Task>(), 0, query.PageIndex, query.PageSize);
        }
    }

    // IUserService and UserService removed - moved to UserService.cs

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

    public interface IConfigService
    {
        string GetValue(string category, string key, string defaultValue = null);
        void SetValue(string category, string key, string value, string description, string updateBy);
        IEnumerable<SystemConfig> GetByCategory(string category);
        IEnumerable<SystemConfig> GetAll();
    }

    public class ConfigService : IConfigService
    {
        private readonly IRepository<SystemConfig> _repository;
        private readonly IAuditLogService _auditLog;

        public ConfigService() : this(new InMemoryRepository<SystemConfig>(), null)
        {
        }

        public ConfigService(IRepository<SystemConfig> repository, IAuditLogService auditLogService)
        {
            _repository = repository;
            _auditLog = auditLogService;
        }

        public string GetValue(string category, string key, string defaultValue = null)
        {
            var configs = _repository.Find(x => x.Category == category && x.Key == key && !x.IsDeleted);
            foreach (var config in configs)
            {
                return config.Value;
            }
            return defaultValue;
        }

        public void SetValue(string category, string key, string value, string description, string updateBy)
        {
            var configs = _repository.Find(x => x.Category == category && x.Key == key && !x.IsDeleted);
            SystemConfig config = null;
            foreach (var c in configs)
            {
                config = c;
                break;
            }

            if (config != null)
            {
                config.Value = value;
                config.Description = description;
                config.UpdateBy = updateBy;
                config.UpdateAt = DateTime.Now;
                _repository.Update(config);
                _auditLog?.Log(SessionManager.CurrentUserId, "修改", "Config", 0, $"修改配置: {category}/{key} = {value}");
            }
            else
            {
                _repository.Add(new SystemConfig
                {
                    Category = category,
                    Key = key,
                    Value = value,
                    Description = description,
                    CreateBy = updateBy,
                    UpdateBy = updateBy,
                    CreateAt = DateTime.Now,
                    UpdateAt = DateTime.Now,
                    IsDeleted = false
                });
                _auditLog?.Log(SessionManager.CurrentUserId, "创建", "Config", 0, $"创建配置: {category}/{key} = {value}");
            }
        }

        public IEnumerable<SystemConfig> GetByCategory(string category)
        {
            return _repository.Find(x => x.Category == category && !x.IsDeleted);
        }

        public IEnumerable<SystemConfig> GetAll()
        {
            return _repository.GetAll();
        }
    }
}