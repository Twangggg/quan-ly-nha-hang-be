using FoodHub.Application.Common.Constants;
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

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Run every 30 seconds for better responsiveness
            }

            _logger.LogInformation("Reservation Lifecycle Worker is stopping.");
        }

        private async Task ProcessLifecycleAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var signalRService = scope.ServiceProvider.GetRequiredService<ISignalRService>();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var settingsProvider =
                scope.ServiceProvider.GetRequiredService<IReservationSettingsProvider>();
            var lifecyclePolicy =
                scope.ServiceProvider.GetRequiredService<IReservationLifecyclePolicy>();

            var settings = await settingsProvider.GetOrCreateAsync(cancellationToken);
            var now = lifecyclePolicy.GetBusinessNow();
            var today = DateOnly.FromDateTime(now);
            var currentTime = now.TimeOfDay;
            var graceCutoffTime = currentTime.Subtract(
                TimeSpan.FromMinutes(settings.GracePeriodMinutes)
            );
            var upcomingBufferTime = currentTime.Add(
                TimeSpan.FromMinutes(settings.UpcomingBufferMinutes)
            );

            await unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Mark No-Shows
                var noShowReservations = await unitOfWork
                    .Repository<Reservation>()
                    .Query()
                    .Where(r =>
                        r.Status == ReservationStatus.Booked
                        && (
                            r.ReservationDate < today
                            || (r.ReservationDate == today && r.ReservationTime <= graceCutoffTime)
                        )
                    )
                    .ToListAsync(cancellationToken);

                foreach (var reservation in noShowReservations)
                {
                    _logger.LogInformation(
                        "Marking reservation {ReservationId} as No-Show (Time: {Time})",
                        reservation.ReservationId,
                        reservation.ReservationTime
                    );
                    reservation.MarkNoShow();
                    unitOfWork.Repository<Reservation>().Update(reservation);
                }

                // 2. Fetch all active reservations for today that should be blocking tables
                // window: [graceCutoffTime, upcomingBufferTime]
                var activeReservations = await unitOfWork
                    .Repository<Reservation>()
                    .Query()
                    .Where(r =>
                        r.Status == ReservationStatus.Booked
                        && r.ReservationDate == today
                        && r.ReservationTime > graceCutoffTime
                        && r.ReservationTime <= upcomingBufferTime
                    )
                    .ToListAsync(cancellationToken);

                var tableIdsToReserve = activeReservations
                    .Select(r => r.TableId)
                    .Distinct()
                    .ToList();

                // 3. Find tables that SHOULD be Reserved but are currently Available
                var tablesToReserve = await unitOfWork
                    .Repository<Table>()
                    .Query()
                    .Where(t =>
                        t.Status == TableStatus.Available && tableIdsToReserve.Contains(t.TableId)
                    )
                    .ToListAsync(cancellationToken);

                foreach (var table in tablesToReserve)
                {
                    _logger.LogInformation(
                        "Automatic transition: Table {TableNumber} (ID: {TableId}) -> Reserved",
                        table.TableNumber,
                        table.TableId
                    );
                    table.Status = TableStatus.Reserved;
                    unitOfWork.Repository<Table>().Update(table);
                    await signalRService.NotifyTableStatusChangedAsync(table.TableId, "Reserved");
                }

                // 4. Find tables that are Reserved but NO LONGER have a pending reservation in the window
                var tablesToRelease = await unitOfWork
                    .Repository<Table>()
                    .Query()
                    .Where(t =>
                        t.Status == TableStatus.Reserved && !tableIdsToReserve.Contains(t.TableId)
                    )
                    .ToListAsync(cancellationToken);

                foreach (var table in tablesToRelease)
                {
                    _logger.LogInformation(
                        "Automatic transition: Table {TableNumber} (ID: {TableId}) -> Available (Reservation passed or cancelled)",
                        table.TableNumber,
                        table.TableId
                    );
                    table.Status = TableStatus.Available;
                    unitOfWork.Repository<Table>().Update(table);
                    await signalRService.NotifyTableStatusChangedAsync(table.TableId, "Available");
                }

                await unitOfWork.SaveChangeAsync(cancellationToken);
                await unitOfWork.CommitTransactionAsync();

                // Clear cache if any table status or reservation status was changed
                if (noShowReservations.Any() || tablesToReserve.Any() || tablesToRelease.Any())
                {
                    await cacheService.RemoveByPatternAsync("table:*", cancellationToken);
                    await cacheService.RemoveByPatternAsync("reservation:*", cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error processing reservation lifecycle.");
                throw;
            }
        }
    }
}
