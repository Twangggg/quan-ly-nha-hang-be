using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reservations;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodHub.Infrastructure.BackgroundJobs
{
    public class ReservationLifecycleWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReservationLifecycleWorker> _logger;

        public ReservationLifecycleWorker(
            IServiceProvider serviceProvider,
            ILogger<ReservationLifecycleWorker> logger
        )
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Reservation Lifecycle Worker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessLifecycleAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing Reservation Lifecycle Worker.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("Reservation Lifecycle Worker is stopping.");
        }

        private async Task ProcessLifecycleAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var signalRService = scope.ServiceProvider.GetRequiredService<ISignalRService>();
            var settingsProvider = scope.ServiceProvider.GetRequiredService<IReservationSettingsProvider>();
            var lifecyclePolicy = scope.ServiceProvider.GetRequiredService<IReservationLifecyclePolicy>();

            var settings = await settingsProvider.GetOrCreateAsync(cancellationToken);
            var now = lifecyclePolicy.GetBusinessNow();
            var today = DateOnly.FromDateTime(now);
            var graceCutoffTime = now.TimeOfDay.Subtract(TimeSpan.FromMinutes(settings.GracePeriodMinutes));

            await unitOfWork.BeginTransactionAsync();
            try
            {
                var noShowReservations = await unitOfWork
                    .Repository<Reservation>()
                    .Query()
                    .Include(r => r.Table)
                    .Where(r =>
                        r.Status == ReservationStatus.Booked
                        && (
                            r.ReservationDate < today
                            || (
                                r.ReservationDate == today
                                && r.ReservationTime <= graceCutoffTime
                            )
                        )
                    )
                    .ToListAsync(cancellationToken);

                foreach (var reservation in noShowReservations)
                {
                    reservation.MarkNoShow();
                    unitOfWork.Repository<Reservation>().Update(reservation);
                }

                if (noShowReservations.Count == 0)
                {
                    await unitOfWork.RollbackTransactionAsync();
                    return;
                }

                var tables = await unitOfWork
                    .Repository<Table>()
                    .Query()
                    .Where(t => noShowReservations.Select(r => r.TableId).Contains(t.TableId))
                    .ToListAsync(cancellationToken);

                foreach (var table in tables)
                {
                    if (table.Status == TableStatus.OutOfService)
                    {
                        continue;
                    }

                    table.Status = TableStatus.Available;
                    unitOfWork.Repository<Table>().Update(table);
                }

                await unitOfWork.SaveChangeAsync(cancellationToken);
                await unitOfWork.CommitTransactionAsync();

                foreach (var tableId in noShowReservations.Select(r => r.TableId).Distinct())
                {
                    await signalRService.NotifyTableStatusChangedAsync(tableId, "Available");
                }

                _logger.LogInformation(
                    "Reservation lifecycle worker updated {NoShowCount} no-shows.",
                    noShowReservations.Count
                );
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
