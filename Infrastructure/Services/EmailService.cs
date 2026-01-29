using FoodHub.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace FoodHub.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendPasswordResetEmailAsync(
            string email, 
            string resetLink, 
            string employeeName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var smtpHost = _configuration["Smtp:Host"];
                var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");
                var smtpUsername = _configuration["Smtp:Username"];
                var smtpPassword = _configuration["Smtp:Password"];
                var fromEmail = _configuration["Smtp:FromEmail"];
                var fromName = _configuration["Smtp:FromName"];

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    Timeout = 30000 // 30 seconds
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail!, fromName),
                    Subject = "Đặt lại mật khẩu - FoodHub",
                    Body = GetEmailTemplate(employeeName, resetLink),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogInformation("Password reset email sent successfully to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
                return false;
            }
        }

        private string GetEmailTemplate(string employeeName, string resetLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔐 Đặt lại mật khẩu</h1>
        </div>
        <div class=""content"">
            <p>Xin chào <strong>{employeeName}</strong>,</p>
            
            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn tại hệ thống FoodHub.</p>
            
            <p>Để đặt lại mật khẩu, vui lòng nhấn vào nút bên dưới:</p>
            
            <div style=""text-align: center;"">
                <a href=""{resetLink}"" class=""button"">Đặt lại mật khẩu</a>
            </div>
            
            <p>Hoặc copy link sau vào trình duyệt:</p>
            <p style=""word-break: break-all; background: white; padding: 10px; border-radius: 5px;"">{resetLink}</p>
            
            <div class=""warning"">
                <strong>⚠️ Lưu ý:</strong>
                <ul>
                    <li>Link này chỉ có hiệu lực trong <strong>15 phút</strong></li>
                    <li>Link chỉ sử dụng được <strong>một lần</strong></li>
                    <li>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này</li>
                </ul>
            </div>
            
            <p>Trân trọng,<br><strong>FoodHub System</strong></p>
        </div>
        <div class=""footer"">
            <p>Email này được gửi tự động. Vui lòng không trả lời email này.</p>
            <p>&copy; 2026 FoodHub. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
