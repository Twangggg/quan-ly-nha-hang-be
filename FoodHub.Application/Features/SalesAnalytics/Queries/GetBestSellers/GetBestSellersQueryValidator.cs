using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetBestSellers;

public class GetBestSellersQueryValidator : AbstractValidator<GetBestSellersQuery>
{
    public GetBestSellersQueryValidator(IMessageService messageService)
    {
        // Top phải trong khoảng 1–100
        RuleFor(x => x.Top)
            .GreaterThan(0)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.TopMustBeGreaterThanZero));

        RuleFor(x => x.Top)
            .LessThanOrEqualTo(100)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.TopMustNotExceed100));

        // EndDate phải >= StartDate (nếu cả hai được cung cấp)
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.EndDateMustBeAfterStartDate));

        // StartDate không được ở tương lai
        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .When(x => x.StartDate.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.DateNotInFuture));

        // Nếu có EndDate thì phải có StartDate
        RuleFor(x => x.StartDate)
            .NotNull()
            .When(x => x.EndDate.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.StartDateRequiredWithEndDate));
    }
}
