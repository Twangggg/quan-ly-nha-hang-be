using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Vouchers.Commands.UpdateVoucher
{
    public record UpdateVoucherCommand(
        Guid VoucherId,
        string VoucherCode,
        VoucherType VoucherType,
        decimal? DiscountValue,
        decimal? MaxDiscount,
        decimal? MinOrderValue,
        Guid? ItemtId,
        int? FreeQuantity,
        DateTime StartDate,
        DateTime EndDate,
        TimeSpan? StartTime,
        TimeSpan? EndTime,
        int? UsageLimit,
        bool IsActive
        ) : IRequest<Result<UpdateVoucherResponse>>;
}
