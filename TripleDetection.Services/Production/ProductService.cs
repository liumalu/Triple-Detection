using System;
using System.Collections.Generic;
using System.Linq;
using TripleDetection.Data;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories;
using TripleDetection.Data.Repositories.Sqlite;
using TripleDetection.Services.Audit;

namespace TripleDetection.Services.Production
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

        public ProductService() : this(new SqliteRepositoryFactory().CreateRepository<Product>(), null)
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
            System.Diagnostics.Debug.WriteLine($"[ProductService.Create] 开始 | Name={product.Name}");
            product.CreateBy = createBy;
            product.UpdateBy = createBy;
            product.CreateAt = DateTime.Now;
            product.UpdateAt = DateTime.Now;
            product.IsDeleted = false;
            _repository.Add(product);
            System.Diagnostics.Debug.WriteLine($"[ProductService.Create] _repository.Add 完成");
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
            return _repository.Query(query);
        }

        public IPagedResult<Product> Query(ProductQuery query)
        {
            return _repository.Query(query);
        }
    }
}