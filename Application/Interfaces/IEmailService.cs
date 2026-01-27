namespace FoodHub.Application.Interfaces
{
    public interface IEmailService
    {
        //IGenericRepository<T> Repository<T>() where T : class;
        Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default);

    }
}