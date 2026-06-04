using TripleDetection.Domain.Entities;

namespace TripleDetection.Domain.Repositories
{

public enum DatabaseProviderType
{
    InMemory,
    Sqlite,
    MySql,
    PostgreSql,
    SqlServer
}

public interface IRepositoryFactory
{
    IUnitOfWork CreateUnitOfWork();
    IRepository<T> CreateRepository<T>() where T : BaseEntity;
    DatabaseProviderType ProviderType { get; }
}
}