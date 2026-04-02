using System.Text.Json.Serialization;
using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Billing.Commands.CheckoutOrder
{
    public class CheckoutOrderCommand : IRequest<Result<Guid>>
    {
        [JsonIgnore]
        public Guid OrderId { get; set; }

        /// <summary>
        /// Danh sách các dòng thanh toán. Tổng Amount phải bằng TotalAmount của Order.
        /// </summary>
        public List<PaymentLineDto> PaymentLines { get; set; } = new();

        // Backward-compatible fields for older frontend payloads.
        [JsonPropertyName("paymentMethod")]
        public PaymentMethod? LegacyPaymentMethod { get; set; }

        [JsonPropertyName("amountReceived")]
        public decimal? LegacyAmountReceived { get; set; }
    }

    public class PaymentLineDto
    {
        /// <summary>
        /// ID của PaymentMethodConfig (lấy từ GET /api/v1/payment-methods).
        /// </summary>
        public Guid PaymentMethodConfigId { get; set; }

        /// <summary>
        /// Số tiền thanh toán bằng phương thức này.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Số tiền khách đưa (chỉ dùng cho tiền mặt, để tính tiền thừa). Nullable.
        /// </summary>
        public decimal? AmountReceived { get; set; }
    }
}
