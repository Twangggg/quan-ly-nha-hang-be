namespace FoodHub.Application.Features.Vouchers.Commands.ApplyVoucher
{
    public class ApplyVoucherResponse
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; }
        public Guid VoucherId { get; set; }
        public string VoucherCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
