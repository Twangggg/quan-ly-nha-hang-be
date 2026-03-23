using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Vouchers.Queries.GetVoucherById
{
    public record GetVoucherByIdQuery(Guid VoucherId) : IRequest<Result<GetVoucherByIdResponse>>;
}
