using System.Threading.Channels;
using FoodHub.Application.Interfaces;
using FoodHub.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace FoodHub.Infrastructure.Services
{
    /// <summary>
    /// Triển khai một hàng đợi trong bộ nhớ (In-memory Queue) cho Email sử dụng System.Threading.Channels.
    /// Cho phép ứng dụng đẩy email vào hàng đợi và tiếp tục xử lý các việc khác mà không cần đợi gửi mail xong.
    /// </summary>
    public class BackgroundEmailChannel : IBackgroundEmailSender
    {
        private readonly Channel<EmailMessage> _channel;
        private readonly ILogger<BackgroundEmailChannel> _logger;

        public BackgroundEmailChannel(ILogger<BackgroundEmailChannel> logger)
        {
            _logger = logger;
            // Tạo một kênh (Channel) có giới hạn 100 thông báo để tránh tràn bộ nhớ nếu email gửi quá chậm
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait // Nếu hàng đợi đầy, lệnh ghi vào sẽ phải đợi (Wait)
            };
            _channel = Channel.CreateBounded<EmailMessage>(options);
        }

        /// <summary>
        /// Đẩy một yêu cầu gửi email vào hàng đợi
        /// </summary>
        public async ValueTask EnqueueEmailAsync(string recipientEmail, string subject, string body, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken)
        {
            var message = new EmailMessage
            {
                To = recipientEmail,
                Subject = subject,
                Body = body,
                AuditTargetId = auditTargetId,
                PerformedByEmployeeId = performedByEmployeeId
            };

            // Ghi message vào Channel, sau đó EmailBackgroundWorker sẽ đọc và xử lý sau
            await _channel.Writer.WriteAsync(message, cancellationToken);
            _logger.LogInformation("Email to {Email} queued for background delivery.", recipientEmail);
        }

        // Cung cấp Reader cho Worker sử dụng
        public ChannelReader<EmailMessage> Reader => _channel.Reader;

        public async ValueTask EnqueueAccountCreationEmailAsync(string email, string employeeName, string employeeCode, string role, string password, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetAccountCreationTemplate(employeeName, employeeCode, role, password);
            await EnqueueEmailAsync(email, "Chào mừng đến FoodHub - Thông tin tài khoản", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }

        public async ValueTask EnqueueRoleChangeEmailAsync(string email, string employeeName, string oldEmployeeCode, string newEmployeeCode, string oldRole, string newRole, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetRoleChangeTemplate(employeeName, oldEmployeeCode, newEmployeeCode, oldRole, newRole);
            await EnqueueEmailAsync(email, "Thông báo thay đổi vai trò - FoodHub", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }

        public async ValueTask EnqueuePasswordResetByManagerEmailAsync(string email, string employeeName, string employeeCode, string newPassword, string managerName, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetPasswordResetByManagerTemplate(employeeName, employeeCode, newPassword, managerName);
            await EnqueueEmailAsync(email, "Mật khẩu của bạn đã được reset - FoodHub", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }

        public async ValueTask EnqueuePasswordResetEmailAsync(string email, string resetLink, string employeeName, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetPasswordResetTemplate(employeeName, resetLink);
            await EnqueueEmailAsync(email, "Password Reset - FoodHub", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }
    }
}
