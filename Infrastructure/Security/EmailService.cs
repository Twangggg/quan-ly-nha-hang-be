using FoodHub.Application.Interfaces;
using Microsoft.Extensions.Options;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;

namespace FoodHub.Infrastructure.Security
{
    public class EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger) : IEmailService
    {
        private readonly ILogger<EmailService> _logger = logger;
        private readonly EmailSettings _emailSettings = emailSettings.Value;


        public async System.Threading.Tasks.Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
        {

            if (string.IsNullOrWhiteSpace(to)) throw new ArgumentNullException(nameof(to));
            if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentNullException(nameof(subject));
            if (string.IsNullOrWhiteSpace(body)) throw new ArgumentNullException(nameof(body));

            if (!IsValidEmail(to))
            {
                throw new ArgumentException("Invalid email format", nameof(to));
            }

            ct.ThrowIfCancellationRequested();
            var config = new Configuration();
            if (!string.IsNullOrEmpty(_emailSettings.ApiUrl))
            {
                config.BasePath = _emailSettings.ApiUrl;
            }

            if (config.ApiKey.ContainsKey("api-key"))
            {
                config.ApiKey["api-key"] = _emailSettings.ApiKey;
            }
            else
            {
                config.ApiKey.Add("api-key", _emailSettings.ApiKey);
            }


            var apiInstance = new TransactionalEmailsApi(config);

            var sendSmtpEmail = new SendSmtpEmail(
                sender: new SendSmtpEmailSender(_emailSettings.SenderName, _emailSettings.SenderEmail),
                to: new List<SendSmtpEmailTo> { new SendSmtpEmailTo(to) },
                subject: subject,
                htmlContent: body
            );

            try
            {

                ct.ThrowIfCancellationRequested();

                var result = await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
                _logger.LogInformation("Email sent successfully to {Email}. MessageId: {MessageId}", to, result.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FAILED to send email to {Email}", to);
                throw;
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
