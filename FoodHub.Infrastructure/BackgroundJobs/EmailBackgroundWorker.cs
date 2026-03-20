using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Infrastructure.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodHub.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Worker chạy ngầm (Background Service) chuyên xử lý việc gửi Email
    /// Giúp API phản hồi nhanh mà không cần đợi gửi mail xong (Fire-and-Forget)
    /// </summary>
    public class EmailBackgroundWorker : BackgroundService
    {
        private readonly BackgroundEmailChannel _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailBackgroundWorker> _logger;

        public EmailBackgroundWorker(
            BackgroundEmailChannel channel,
            IServiceProvider serviceProvider,
            ILogger<EmailBackgroundWorker> logger)
        {
            _channel = channel;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Phương thức chạy chính của Worker. Nó sẽ lặp vô hạn cho đến khi ứng dụng tắt.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EmailBackgroundWorker started.");

            // Đọc dữ liệu từ Channel (hàng đợi trong bộ nhớ)
            // Lệnh này sẽ treo và đợi cho đến khi có email mới được đẩy vào hàng đợi
            await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessEmailAsync(message, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing email for {Email}", message.To);
                }
            }

            _logger.LogInformation("EmailBackgroundWorker stopped.");
        }

        /// <summary>
        /// Xử lý gửi 1 email cụ thể với chính sách thử lại (Retry)
        /// </summary>
        private async Task ProcessEmailAsync(EmailMessage message, CancellationToken stoppingToken)
        {
            // Vì Worker là Singleton, chúng ta cần tạo Scope mới để dùng được các Service Scoped (như DbContext)
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // 1. Chính sách thử lại (Retry Policy)
            int maxRetries = 3;             // Thử lại tối đa 3 lần nếu lỗi
            int delay = 1000;              // Thời gian chờ ban đầu 1 giây
            bool success = false;
            Exception? lastException = null;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    await emailService.SendEmailAsync(message.To, message.Subject, message.Body, stoppingToken);
                    success = true;
                    break; // Nếu gửi thành công thì thoát vòng lặp retry
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning("Attempt {Attempt} failed to send email to {Email}: {Message}", i + 1, message.To, ex.Message);

                    // Chờ trước khi thử lại - Exponential Backoff (1s -> 2s -> 4s)
                    await Task.Delay(delay, stoppingToken);
                    delay *= 2;
                }
            }

            // 2. Nếu sau tất cả các lần thử vẫn thất bại, ghi log lỗi vào database (AuditLog)
            if (!success)
            {
                _logger.LogError(lastException, "FAILED to send email to {Email} after {Retries} attempts.", message.To, maxRetries);

                if (message.AuditTargetId.HasValue && message.PerformedByEmployeeId.HasValue)
                {
                    await LogFailureToAudit(scope, message, lastException?.Message ?? "Unknown error");
                }
            }
        }

        private async Task LogFailureToAudit(IServiceScope scope, EmailMessage message, string errorDetails)
        {
            try
            {
                var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
                await auditLogService.LogActivityAsync(
                    AuditAction.EmailFailure,
                    "Email",
                    $"{{\"AuditTargetId\":\"{message.AuditTargetId}\"}}",
                    null,
                    new
                    {
                        To = message.To,
                        Subject = message.Subject,
                        Error = errorDetails,
                        PerformedByEmployeeId = message.PerformedByEmployeeId
                    });

                _logger.LogInformation("Logged failure to AuditLog for target {Target}", message.AuditTargetId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CRITICAL: Failed to log usage failure to AuditLog!");
            }
        }
    }
}
