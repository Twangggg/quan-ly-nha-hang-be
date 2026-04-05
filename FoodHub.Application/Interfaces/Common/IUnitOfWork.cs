namespace FoodHub.Application.Interfaces.Common
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<T> Repository<T>() where T : class;
        Task<int> SaveChangeAsync(CancellationToken ct = default);
        void ClearChangeTracker();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
