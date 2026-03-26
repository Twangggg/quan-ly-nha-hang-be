using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Invoice : BaseEntity
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

        public string CashierName { get; set; } = string.Empty;

        public string? TableNumber { get; set; }

        // Dữ liệu chi tiết các món
        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }
}
