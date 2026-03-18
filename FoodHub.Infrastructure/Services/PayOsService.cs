using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using System.Text.Json;

namespace FoodHub.Infrastructure.Services
{
    public class PayOsService : IPaymentService
    {
        private readonly PayOSClient _payOs;
        private readonly PayOsSettings _settings;

        public PayOsService(IOptions<PayOsSettings> options)
        {
            _settings = options.Value;
            var payOsOptions = new PayOSOptions
            {
                ClientId = _settings.ClientId,
                ApiKey = _settings.ApiKey,
                ChecksumKey = _settings.ChecksumKey
            };
            _payOs = new PayOSClient(payOsOptions);
        }

        public async Task<PaymentLinkResponse> CreatePaymentLinkAsync(Order order, CancellationToken token = default)
        {
            var amount = (long)Math.Max(1000, order.TotalAmount); // minimum amount rule for test
            
            var request = new CreatePaymentLinkRequest
            {
                OrderCode = order.TransactionCode,
                Amount = amount,
                Description = $"Thanh toan don {order.TransactionCode}",
                CancelUrl = _settings.CancelUrl,
                ReturnUrl = _settings.ReturnUrl
            };

            var createPaymentResult = await _payOs.PaymentRequests.CreateAsync(request);

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
            
            var verifiedData = await _payOs.Webhooks.VerifyAsync(webhook);
            if (verifiedData == null) throw new UnauthorizedAccessException("Webhook verification failed");
            
            return verifiedData.OrderCode;
        }
    }
}
