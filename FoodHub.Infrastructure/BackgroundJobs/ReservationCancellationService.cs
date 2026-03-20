using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
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
            ILogger<ReservationCancellationService> logger
        )
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
                    _logger.LogError(
                        ex,
                        "Error occurred executing Reservation Cancellation Service."
                    );
                }

                // Chạy mỗi 10p một lần
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }

            _logger.LogInformation("Reservation Cancellation Service is stopping.");
        }

        private async Task CancelOverdueReservationsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Calculate the threshold: 30 minutes past today's reservation time
            var today = DateOnly.FromDateTime(DateTime.Now);
            var overdueTime = DateTime.Now.TimeOfDay.Subtract(TimeSpan.FromMinutes(15));

            // Only touch the columns we actually need so older DB schemas do not break the job.
            var overdueReservations = unitOfWork
                .Repository<Reservation>()
                .Query()
                .Where(r =>
                    r.Status == ReservationStatus.Booked
                    && r.ReservationDate == today
                    && r.ReservationTime <= overdueTime
                );

            var overdueCount = await overdueReservations.CountAsync(cancellationToken);
            if (overdueCount == 0)
            {
                return;
            }

            _logger.LogInformation(
                "Found {Count} overdue reservations. Proceeding to cancel.",
                overdueCount
            );

            await overdueReservations.ExecuteUpdateAsync(
                setters => setters.SetProperty(r => r.Status, ReservationStatus.Cancelled),
                cancellationToken
            );

            _logger.LogInformation(
                "Successfully cancelled {Count} overdue reservations.",
                overdueCount
            );
        }
    }
}
