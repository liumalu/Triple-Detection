using System;
using TripleDetection.Domain.Entities;

namespace TripleDetection.Domain.Repositories;

public interface IUnitOfWork : IDisposable
{
    void BeginTransaction();
    void Commit();
    void Rollback();
    IRepository<T> GetRepository<T>() where T : BaseEntity;
    int SaveChanges();
    bool IsInTransaction { get; }
}