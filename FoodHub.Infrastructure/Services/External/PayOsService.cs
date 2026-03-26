using System.Text.Json;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly PayOsSettings _settings;

        public PayOsService(IUnitOfWork unitOfWork, IOptions<PayOsSettings> options)
        {
            _unitOfWork = unitOfWork;
            _settings = options.Value;
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

            var payOsOptions = new PayOSOptions
            {
                ClientId = config.PayOsClientId,
                ApiKey = config.PayOsApiKey,
                ChecksumKey = config.PayOsChecksumKey
            };

            return new PayOSClient(payOsOptions);
        }

        public async Task<PaymentLinkResponse> CreatePaymentLinkAsync(Order order, CancellationToken token = default)
        {
            var payOs = await GetDynamicPayOSClientAsync(token);
            var amount = (long)Math.Max(1000, order.TotalAmount); // minimum amount rule for test

            var request = new CreatePaymentLinkRequest
            {
                OrderCode = order.TransactionCode,
                Amount = amount,
                Description = $"Thanh toan don {order.TransactionCode}",
                CancelUrl = _settings.CancelUrl,
                ReturnUrl = _settings.ReturnUrl
            };

            var createPaymentResult = await payOs.PaymentRequests.CreateAsync(request);

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

            var payOs = await GetDynamicPayOSClientAsync();
            var verifiedData = await payOs.Webhooks.VerifyAsync(webhook);
            if (verifiedData == null) throw new UnauthorizedAccessException("Webhook verification failed");

            return verifiedData.OrderCode;
        }
    }
}
