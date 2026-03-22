namespace FoodHub.Application.Features.Vouchers.Commands.ApplyVoucher
{
    public class ApplyVoucherResponse
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; }
        public Guid? OldVoucherId { get; set; }
        public string? OldVoucherCode { get; set; }
        public Guid NewVoucherId { get; set; }
        public string NewVoucherCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
