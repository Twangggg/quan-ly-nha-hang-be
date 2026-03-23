using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodHub.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Background service chạy mỗi 1 phút để phát hiện đặt bàn đã đến giờ
    /// và thông báo realtime qua SignalR để FE cập nhật sơ đồ bàn.
    /// </summary>
    public class TableStatusSyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TableStatusSyncService> _logger;

        public TableStatusSyncService(
            IServiceProvider serviceProvider,
            ILogger<TableStatusSyncService> logger
        )
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Table Status Sync Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncTableStatusAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in Table Status Sync Service.");
                }

                // Chạy mỗi 1 phút
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("Table Status Sync Service is stopping.");
        }

        private async Task SyncTableStatusAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var signalRService = scope.ServiceProvider.GetRequiredService<ISignalRService>();

            var vietnamTime = DateTime.UtcNow.AddHours(7);
            var today = DateOnly.FromDateTime(vietnamTime);
            var currentTime = vietnamTime.TimeOfDay;

            // Tìm các đặt bàn đã đến giờ (Status = Booked, ngày hôm nay, giờ đặt <= giờ hiện tại)
            var dueReservations = await unitOfWork
                .Repository<Reservation>()
                .Query()
                .Where(r =>
                    r.Status == ReservationStatus.Booked
                    && r.ReservationDate == today
                    && r.ReservationTime <= currentTime
                )
                .ToListAsync(cancellationToken);

            if (!dueReservations.Any())
                return;

            _logger.LogInformation(
                "Found {Count} reservations that have reached their time. Broadcasting table status update.",
                dueReservations.Count
            );

            // Broadcast "Reserved" status to FE for each table
            foreach (var reservation in dueReservations)
            {
                await signalRService.NotifyTableStatusChangedAsync(
                    reservation.TableId,
                    "Reserved"
                );
            }
        }
    }
}
