using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodHub.Application.Features.Vouchers.Commands.UnapplyVoucher
{
    public class UnapplyVoucherResponse
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; }
        public Guid OldVoucherId { get; set; }
        public string OldVoucherCode { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
