namespace FoodHub.Application.Features.Billing.Queries.GetPreCheckBill
{
    /// <summary>
    /// Thông tin phiếu tạm tính dùng cho preview và xuất PDF.
    /// </summary>
    public class GetPreCheckBillResponse
    {
        /// <summary>
        /// ID đơn hàng.
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Mã đơn hàng hiển thị trên phiếu.
        /// </summary>
        public string OrderCode { get; set; } = null!;

        /// <summary>
        /// Số bàn, null nếu là mang về.
        /// </summary>
        public int? TableNumber { get; set; }

        /// <summary>
        /// Tên nhân viên tạo đơn hoặc phục vụ đơn.
        /// </summary>
        public string EmployeeName { get; set; } = null!;

        /// <summary>
        /// Thời điểm tạo phiếu tạm tính.
        /// </summary>
        public DateTime PrintedAt { get; set; }

        /// <summary>
        /// Danh sách món hợp lệ trên phiếu.
        /// </summary>
        public List<PreCheckBillItemDto> Items { get; set; } = new();

        /// <summary>
        /// Tạm tính trước giảm giá, VAT.
        /// </summary>
        public decimal SubTotal { get; set; }

        /// <summary>
        /// Tổng số tiền giảm giá áp dụng.
        /// </summary>
        public decimal Discount { get; set; }

        /// <summary>
        /// Tổng VAT áp dụng.
        /// </summary>
        public decimal Vat { get; set; }

        /// <summary>
        /// Số tiền khách cần thanh toán.
        /// </summary>
        public decimal TotalAmount { get; set; }
    }

    /// <summary>
    /// Một dòng món trên phiếu tạm tính.
    /// </summary>
    public class PreCheckBillItemDto
    {
        /// <summary>
        /// Tên món.
        /// </summary>
        public string ItemName { get; set; } = null!;

        /// <summary>
        /// Số lượng món.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Đơn giá của món.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Tóm tắt các tuỳ chọn đi kèm.
        /// </summary>
        public string? OptionsSummary { get; set; }

        /// <summary>
        /// Thành tiền của dòng món.
        /// </summary>
        public decimal LineTotal { get; set; }
    }
}
