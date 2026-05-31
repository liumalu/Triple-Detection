using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Entities.Queries;

namespace TripleDetection.Domain.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    T? GetById(int id);
    IEnumerable<T> GetAll();
    IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
    void Add(T entity);
    void Update(T entity);
    void Delete(int id);
    int Count();
    int Count(Expression<Func<T, bool>> predicate);
    IPagedResult<T> Query(PagedQuery query);
}