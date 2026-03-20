using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Vouchers.Commands.DeleteVoucher
{
    public record DeleteVoucherCommand(Guid VoucherId) : IRequest<Result<DeleteVoucherResponse>>;
}
