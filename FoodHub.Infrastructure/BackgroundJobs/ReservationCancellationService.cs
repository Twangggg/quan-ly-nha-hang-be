using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodHub.Infrastructure.BackgroundJobs
{
    public class ReservationCancellationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReservationCancellationService> _logger;

        public ReservationCancellationService(
            IServiceProvider serviceProvider, 
            ILogger<ReservationCancellationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Reservation Cancellation Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CancelOverdueReservationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing Reservation Cancellation Service.");
                }

                // Chạy mỗi 10 phút một lần
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }

            _logger.LogInformation("Reservation Cancellation Service is stopping.");
        }

        private async Task CancelOverdueReservationsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Calculate the threshold: 30 minutes past today's reservation time
            var today = DateOnly.FromDateTime(DateTime.Now);
            var overdueTime = DateTime.Now.TimeOfDay.Subtract(TimeSpan.FromMinutes(30));

            // Get all booked tables where customers haven't arrived (Status = Booked) for today, and are 30 minutes overdue
            var overdueReservations = await unitOfWork.Repository<Reservation>().Query()
                .Where(r => r.Status == ReservationStatus.Booked
                            && r.ReservationDate == today
                            && r.ReservationTime <= overdueTime)
                .ToListAsync(cancellationToken);

            if (!overdueReservations.Any())
            {
                return;
            }

            _logger.LogInformation("Found {Count} overdue reservations. Proceeding to cancel.", overdueReservations.Count);

            foreach (var reservation in overdueReservations)
            {
                _logger.LogInformation("Cancelling Reservation {ReservationId} (Time: {Time}). Customer did not check-in after 30 minutes.", reservation.ReservationId, reservation.ReservationTime);
                
                reservation.Status = ReservationStatus.NoShow;
            }

            await unitOfWork.SaveChangeAsync(cancellationToken);
            _logger.LogInformation("Successfully cancelled {Count} overdue reservations.", overdueReservations.Count);
        }
    }
}
