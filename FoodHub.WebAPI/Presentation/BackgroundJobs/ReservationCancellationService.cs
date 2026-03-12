using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodHub.WebAPI.Presentation.BackgroundJobs
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

            // Tính mốc thời gian: quá 30 phút so với giờ hẹn hôm nay
            var today = DateOnly.FromDateTime(DateTime.Now);
            var overdueTime = DateTime.Now.TimeOfDay.Subtract(TimeSpan.FromMinutes(30));

            // Lấy tất cả bàn đã đặt nhưng khách chưa đến (Status = Booked) của ngày hôm nay, và đã trễ 30 phút
            var overdueReservations = await unitOfWork.Repository<Reservation>().Query()
                .Include(r => r.Order)
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
                _logger.LogInformation("Cancelling Reservation {ReservationId} (Time: {Time}). Khách chưa đến sau 30 phút.", reservation.ReservationId, reservation.ReservationTime);
                
                reservation.Status = ReservationStatus.Cancelled;

                // Nếu có Pre-order (đặt món/combo trước), huỷ luôn Order
                if (reservation.Order != null && reservation.Order.Status == OrderStatus.Pending)
                {
                    reservation.Order.Status = OrderStatus.Cancelled;
                    reservation.Order.Note = (reservation.Order.Note + " [Auto-cancelled due to late arrival]").Trim();
                }
            }

            await unitOfWork.SaveChangeAsync(cancellationToken);
            _logger.LogInformation("Successfully cancelled {Count} overdue reservations.", overdueReservations.Count);
        }
    }
}
