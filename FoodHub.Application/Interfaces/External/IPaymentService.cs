using FoodHub.Domain.Entities;

namespace FoodHub.Application.Interfaces.External
{
    public class PaymentLinkResponse
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
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
