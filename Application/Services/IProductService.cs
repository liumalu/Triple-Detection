using TripleDetection.Domain.Entities;

namespace TripleDetection.Application.Services;

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