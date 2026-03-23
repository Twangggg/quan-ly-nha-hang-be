using FoodHub.Domain.Entities;

namespace FoodHub.Application.Interfaces.External
{
    public class PaymentLinkResponse
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
        public string Bin { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
    }

    public interface IPaymentService
    {
        Task<PaymentLinkResponse> CreatePaymentLinkAsync(Order order, CancellationToken token = default);

        /// <summary>
        /// Verify the webhook data and return the orderCode implicitly or explicitly.
        /// Throws exception if signature is invalid.
        /// </summary>
        Task<long> VerifyWebhookDataAsync(string webhookBody);
    }
}
