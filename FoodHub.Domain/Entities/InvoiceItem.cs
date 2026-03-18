namespace FoodHub.Domain.Entities
{
    public class InvoiceItem : BaseEntity
    {
        public Guid InvoiceItemId { get; set; }

        public Guid InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        // Snapshot Data (Dữ liệu chụp tại thời điểm thanh toán để bảo vệ lịch sử - AC-03)
        // Lưu tên món ăn thay vì Id của món ăn
        public string ItemName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        // Giá tại thời điểm thanh toán
        public decimal UnitPrice { get; set; }

        // Bằng Quantity * UnitPrice
        public decimal TotalPrice { get; set; }

        // Ghi chú lúc đặt món (nếu cần hiển thị trên hóa đơn)
        public string? Note { get; set; }
    }
}
