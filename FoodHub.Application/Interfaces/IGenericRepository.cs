using System.Linq.Expressions;

namespace FoodHub.Application.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(object id);
        IQueryable<T> Query();
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    }
}
