using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetRevenueChart;

public class GetRevenueChartQueryValidator : AbstractValidator<GetRevenueChartQuery>
{
    public GetRevenueChartQueryValidator(IMessageService messageService)
    {
        // Rule 1: Date và Year/Month không được gửi đồng thời
        // Nếu có Date → không được có Year hoặc Month
        RuleFor(x => x.Date)
            .Must((query, date) => !date.HasValue || (!query.Year.HasValue && !query.Month.HasValue))
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.CannotCombineDateWithYearMonth));

        // Rule 2: Nếu có Year thì phải có Month (và ngược lại)
        RuleFor(x => x.Year)
            .NotNull()
            .When(x => x.Month.HasValue && !x.Date.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.YearRequiredWithMonth));

        RuleFor(x => x.Month)
            .NotNull()
            .When(x => x.Year.HasValue && !x.Date.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.MonthRequiredWithYear));

        // Rule 3: Month phải hợp lệ (1–12) nếu được cung cấp
        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .When(x => x.Month.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.MonthMustBeBetween1And12));

        // Rule 4: Year phải là số dương hợp lý nếu được cung cấp
        RuleFor(x => x.Year)
            .GreaterThan(2000)
            .LessThanOrEqualTo(DateTime.UtcNow.Year + 1)
            .When(x => x.Year.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.YearMustBePositive));

        // Rule 5: Date không được ở tương lai (xa quá 1 ngày tính từ UTC)
        RuleFor(x => x.Date)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .When(x => x.Date.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.DateNotInFuture));
    }
}
