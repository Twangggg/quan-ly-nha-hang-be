using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Features.Invoices.Queries.GetInvoiceById
{
    public class GetInvoiceByIdResponse
    {
        public Guid InvoiceId { get; set; }
        // Tham chiếu đến đơn hàng ban đầu
        public Guid OrderId { get; set; }

        // Mã hóa đơn hiển thị (VD: INV-240315-001)
        public string InvoiceNumber { get; set; } = string.Empty;

        // --- Thông tin tài chính ---
        // Tổng tiền các món
        public decimal SubTotal { get; set; }

        // Thuế, giảm giá (nếu có)
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }

        // Tổng tiền cuối cùng khách phải trả
        public decimal TotalAmount { get; set; }

        // Nếu có theo dõi tiền khách đưa và tiền thừa
        public decimal? AmountReceived { get; set; }
        public decimal? AmountReturned { get; set; }

        // --- Thông tin thanh toán ---
        // Phương thức: Cash, BankTransfer, Card
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public string PaymentMethodCode { get; set; } = string.Empty;

        public string CashierName { get; set; } = string.Empty;

        public string TableNumber { get; set; } = string.Empty;

        // Dữ liệu chi tiết các món
        public ICollection<InvoiceItemResponse> Items { get; set; } = new List<InvoiceItemResponse>();
    }

    public class InvoiceItemResponse
    {
        public Guid InvoiceItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Note { get; set; }
    }
}
