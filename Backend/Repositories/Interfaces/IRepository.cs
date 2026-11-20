using System.Linq.Expressions;

namespace Backend.Repositories.Interfaces
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> Query();
        IQueryable<T> QueryAsNoTracking();
        Task<T?> GetByIdAsync(long id);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task<int> SaveChangesAsync();
    }
}

