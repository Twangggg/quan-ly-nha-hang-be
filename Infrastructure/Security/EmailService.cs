using FoodHub.Application.Interfaces;
using Microsoft.Extensions.Options;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using System.Diagnostics;

namespace FoodHub.Infrastructure.Security
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _logger = logger;
            _emailSettings = emailSettings.Value;

            if (!Configuration.Default.ApiKey.ContainsKey("api-key"))
            {
                Configuration.Default.ApiKey.Add("api-key", _emailSettings.ApiKey);
            }
            else
            {
                Configuration.Default.ApiKey["api-key"] = _emailSettings.ApiKey;
            }
        }

        public async System.Threading.Tasks.Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
        {
            var apiInstance = new TransactionalEmailsApi();

            var sendSmtpEmail = new SendSmtpEmail(
                sender: new SendSmtpEmailSender(_emailSettings.SenderName, _emailSettings.SenderEmail),
                to: new List<SendSmtpEmailTo> { new SendSmtpEmailTo(to) },
                subject: subject,
                htmlContent: body
            );

            try
            {
                // SendTransacEmailAsync does not support CancellationToken out of the box in some versions, 
                // but we can await the Task.
                var result = await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
                _logger.LogInformation("Email sent successfully to {Email}. MessageId: {MessageId}", to, result.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FAILED to send email to {Email}", to);
                throw;
            }
        }
    }
}
