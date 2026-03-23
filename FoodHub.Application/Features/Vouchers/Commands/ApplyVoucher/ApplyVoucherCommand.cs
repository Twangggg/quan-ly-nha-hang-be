using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Vouchers.Commands.ApplyVoucher
{
    public record ApplyVoucherCommand(
        Guid OrderId,
        Guid? VoucherId
        ) : IRequest<Result<ApplyVoucherResponse>>;
}
