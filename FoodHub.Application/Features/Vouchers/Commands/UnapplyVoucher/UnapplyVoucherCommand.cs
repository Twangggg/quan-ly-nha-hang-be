using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Vouchers.Commands.UnapplyVoucher
{
    public class UnapplyVoucherCommand : IRequest<Result<UnapplyVoucherResponse>>
    {
        public Guid OrderId { get; set; }
    }
}
