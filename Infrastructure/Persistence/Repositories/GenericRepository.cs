using FoodHub.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FoodHub.Infrastructure.Persistence.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);
        public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
            => _dbSet.AnyAsync(predicate);
        public void DeleteAsync(T entity) => _dbSet.Remove(entity);
        public async Task<T?> GetByIdAsync(object id) => await _dbSet.FindAsync(id);
        public IQueryable<T> Query() => _dbSet.AsQueryable();
        public void UpdateAsync(T entity) => _dbSet.Update(entity);
    }
}
