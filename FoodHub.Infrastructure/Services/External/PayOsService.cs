using System.Text.Json;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using FoodHub.Infrastructure.Settings;

namespace FoodHub.Infrastructure.Services.External
{
    public class PayOsService : IPaymentService
    {
        private readonly PayOsSettings _settings;
<<<<<<< Updated upstream

        public PayOsService(IUnitOfWork unitOfWork, IOptions<PayOsSettings> options)
=======
        private readonly IBrandingSettingsProvider _brandingSettingsProvider;
        private readonly PayOSClient _payOsClient;

        public PayOsService(
            IOptions<PayOsSettings> options,
            IBrandingSettingsProvider brandingSettingsProvider
        )
>>>>>>> Stashed changes
        {
            _settings = options.Value;
<<<<<<< Updated upstream
        }

        private async Task<PayOSClient> GetDynamicPayOSClientAsync(CancellationToken token = default)
        {
            var config = await _unitOfWork.Repository<PaymentMethodConfig>()
                .Query()
                .FirstOrDefaultAsync(x => x.Type == PaymentMethodType.BankTransfer && x.IsActive, token);

            if (config == null || string.IsNullOrWhiteSpace(config.PayOsClientId) || string.IsNullOrWhiteSpace(config.PayOsApiKey) || string.IsNullOrWhiteSpace(config.PayOsChecksumKey))
            {
                throw new InvalidOperationException("PayOS keys are not configured in the active BankTransfer payment method.");
            }

=======
            _brandingSettingsProvider = brandingSettingsProvider;
            
            // Khởi tạo client trực tiếp từ cấu hình
>>>>>>> Stashed changes
            var payOsOptions = new PayOSOptions
            {
                ClientId = _settings.ClientId,
                ApiKey = _settings.ApiKey,
                ChecksumKey = _settings.ChecksumKey
            };
            _payOsClient = new PayOSClient(payOsOptions);
        }

        public async Task<PaymentLinkResponse> CreatePaymentLinkAsync(Order order, decimal amount, CancellationToken token = default)
        {
<<<<<<< Updated upstream
            var payOs = await GetDynamicPayOSClientAsync(token);
            var payAmount = (long)Math.Max(1000, amount); // minimum amount rule for test
=======
            var payAmount = (long)Math.Max(1000, amount);
            var branding = await _brandingSettingsProvider.GetOrCreateAsync(token);
>>>>>>> Stashed changes

            var request = new CreatePaymentLinkRequest
            {
                OrderCode = order.TransactionCode,
                Amount = payAmount,
                Description = $"Thanh toan don {order.TransactionCode}",
                CancelUrl = _settings.CancelUrl,
                ReturnUrl = _settings.ReturnUrl
            };

            var createPaymentResult = await _payOsClient.PaymentRequests.CreateAsync(request);

            return new PaymentLinkResponse
            {
                CheckoutUrl = createPaymentResult.CheckoutUrl ?? string.Empty,
                QrCode = createPaymentResult.QrCode ?? string.Empty,
                Bin = createPaymentResult.Bin ?? string.Empty,
                AccountNumber = createPaymentResult.AccountNumber ?? string.Empty,
                AccountName = createPaymentResult.AccountName ?? string.Empty,
                Amount = createPaymentResult.Amount,
                Description = createPaymentResult.Description ?? string.Empty,
                Currency = createPaymentResult.Currency ?? string.Empty
            };
        }

        public async Task<long> VerifyWebhookDataAsync(string webhookBody)
        {
            var webhook = JsonSerializer.Deserialize<Webhook>(webhookBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (webhook == null) throw new ArgumentException("Invalid webhook data");

            var verifiedData = await _payOsClient.Webhooks.VerifyAsync(webhook);
            if (verifiedData == null) throw new UnauthorizedAccessException("Webhook verification failed");

            return verifiedData.OrderCode;
        }
<<<<<<< Updated upstream
=======

        public async Task<string> GetPaymentStatusAsync(long orderCode, CancellationToken token = default)
        {
            var paymentInfo = await _payOsClient.PaymentRequests.GetAsync(orderCode);
            return paymentInfo.Status.ToString();
        }
>>>>>>> Stashed changes
    }
}
