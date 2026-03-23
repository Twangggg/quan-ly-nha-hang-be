using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Vouchers.Commands.UpdateVoucherActive
{
    public record UpdateVoucherActiveCommand(
        Guid VoucherId,
        bool IsActive
        ) : IRequest<Result<UpdateVoucherActiveResponse>>;
}
