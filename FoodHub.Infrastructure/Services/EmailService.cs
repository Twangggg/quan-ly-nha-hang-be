using FoodHub.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace FoodHub.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _logger = logger;
            _emailSettings = emailSettings.Value;

            if (string.IsNullOrWhiteSpace(_emailSettings.SenderEmail))
            {
                _logger.LogWarning("EmailService: SenderEmail is not configured. Email sending will fail.");
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(
            string email,
            string resetLink,
            string employeeName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.AppPassword),
                    Timeout = 30000 // 30 seconds
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = "Password Reset - FoodHub",
                    Body = EmailTemplates.GetPasswordResetTemplate(employeeName, resetLink),
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

        public async Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(to)) throw new ArgumentNullException(nameof(to));
            if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentNullException(nameof(subject));
            if (string.IsNullOrWhiteSpace(body)) throw new ArgumentNullException(nameof(body));

            if (!IsValidEmail(to))
            {
                throw new ArgumentException("Invalid email format", nameof(to));
            }

            ct.ThrowIfCancellationRequested();

            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.AppPassword),
                    EnableSsl = true
                };

                if (string.IsNullOrWhiteSpace(_emailSettings.SenderEmail))
                {
                    throw new InvalidOperationException("Cannot send email: SenderEmail configuration is missing.");
                }

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(to);

                await smtpClient.SendMailAsync(mailMessage, ct);

                _logger.LogInformation("Email sent successfully to {Email}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FAILED to send email to {Email}", to);
                throw;
            }
        }

        public async Task<bool> SendAccountCreationEmailAsync(
            string email,
            string employeeName,
            string employeeCode,
            string role,
            string password,
            CancellationToken cancellationToken = default)
        {
            if (!IsValidEmail(email))
            {
                throw new ArgumentException("Invalid email format", nameof(email));
            }
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.AppPassword),
                    Timeout = 30000
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = "Chào mừng đến FoodHub - Thông tin tài khoản",
                    Body = EmailTemplates.GetAccountCreationTemplate(employeeName, employeeCode, role, password),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogInformation("Account creation email sent successfully to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send account creation email to {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendRoleChangeConfirmationEmailAsync(
            string email,
            string employeeName,
            string oldEmployeeCode,
            string newEmployeeCode,
            string oldRole,
            string newRole,
            CancellationToken cancellationToken = default)
        {
            if (!IsValidEmail(email))
            {
                throw new ArgumentException("Invalid email format", nameof(email));
            }
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.AppPassword),
                    Timeout = 30000
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = "Thông báo thay đổi vai trò - FoodHub",
                    Body = EmailTemplates.GetRoleChangeTemplate(employeeName, oldEmployeeCode, newEmployeeCode, oldRole, newRole),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogInformation("Role change confirmation email sent successfully to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send role change confirmation email to {Email}", email);
                return false;
            }
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Strict Regex for Email Validation
                var regex = new System.Text.RegularExpressions.Regex(
                    @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }


        public async Task<bool> SendPasswordResetByManagerEmailAsync(
            string email,
            string employeeName,
            string employeeCode,
            string newPassword,
            string managerName,
            CancellationToken cancellationToken = default)
        {
            if (!IsValidEmail(email))
            {
                throw new ArgumentException("Invalid email format", nameof(email));
            }
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.AppPassword),
                    Timeout = 30000
                };
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = "Mật khẩu của bạn đã được reset - FoodHub",
                    Body = EmailTemplates.GetPasswordResetByManagerTemplate(employeeName, employeeCode, newPassword, managerName),
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);
                await smtpClient.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogInformation("Password reset by manager email sent successfully to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset by manager email to {Email}", email);
                return false;
            }
        }
    }
}
