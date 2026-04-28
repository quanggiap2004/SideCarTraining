using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SideCar.Business.Repositories
{
    public interface IRepository<T> where T : class
    {
        //read
        Task<T?> FindByIdAsync(Guid id);
        Task<T?> FindByAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAllByAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> GetAllPageAsync(int pageNumber, int pageSize);
        Task<IEnumerable<T>> GetPageAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? predicate,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy,
            params Expression<Func<T, object>>[]? includes);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
        //write
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void Delete(T entity);
        Task<int> SaveChangesAsync();
    }
}
