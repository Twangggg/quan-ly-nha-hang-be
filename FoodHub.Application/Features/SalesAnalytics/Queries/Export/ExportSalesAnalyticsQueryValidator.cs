using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;

namespace FoodHub.Application.Features.SalesAnalytics.Queries.Export;

public class ExportSalesAnalyticsQueryValidator : AbstractValidator<ExportSalesAnalyticsQuery>
{
    public ExportSalesAnalyticsQueryValidator(IMessageService messageService)
    {
        // Rule 1: Date và Year/Month không được gửi đồng thời
        RuleFor(x => x.Date)
            .Must((query, date) => !date.HasValue || (!query.Year.HasValue && !query.Month.HasValue))
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.CannotCombineDateWithYearMonth));

        // Rule 2: Date và StartDate/EndDate không được gửi đồng thời
        RuleFor(x => x.Date)
            .Must((query, date) => !date.HasValue || (!query.StartDate.HasValue && !query.EndDate.HasValue))
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.CannotCombineDateWithYearMonth));

        // Rule 3: Nếu có Year thì phải có Month (và ngược lại)
        RuleFor(x => x.Year)
            .NotNull()
            .When(x => x.Month.HasValue && !x.Date.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.YearRequiredWithMonth));

        RuleFor(x => x.Month)
            .NotNull()
            .When(x => x.Year.HasValue && !x.Date.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.MonthRequiredWithYear));

        // Rule 4: Month phải hợp lệ (1–12)
        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .When(x => x.Month.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.MonthMustBeBetween1And12));

        // Rule 5: Year phải hợp lý
        RuleFor(x => x.Year)
            .GreaterThan(2000)
            .LessThanOrEqualTo(DateTime.UtcNow.Year + 1)
            .When(x => x.Year.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.YearMustBePositive));

        // Rule 6: EndDate phải >= StartDate
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.EndDateMustBeAfterStartDate));

        // Rule 7: StartDate/EndDate phải đi cặp
        RuleFor(x => x.StartDate)
            .NotNull()
            .When(x => x.EndDate.HasValue && !x.Date.HasValue && !x.Year.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.StartDateRequiredWithEndDate));

        RuleFor(x => x.EndDate)
            .NotNull()
            .When(x => x.StartDate.HasValue && !x.Date.HasValue && !x.Year.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.EndDateRequiredWithStartDate));

        // Rule 8: StartDate không được ở tương lai
        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .When(x => x.StartDate.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.DateNotInFuture));

        // Rule 9: Date không được ở tương lai
        RuleFor(x => x.Date)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .When(x => x.Date.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.DateNotInFuture));
    }
}
