using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetMonthlyReport;

public class GetMonthlyReportQueryValidator : AbstractValidator<GetMonthlyReportQuery>
{
    public GetMonthlyReportQueryValidator(IMessageService messageService)
    {
        // Month phải nằm trong khoảng 1–12 (nếu được cung cấp)
        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .When(x => x.Month.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.MonthMustBeBetween1And12));

        // Year phải là số dương hợp lý (nếu được cung cấp)
        RuleFor(x => x.Year)
            .GreaterThan(2000)
            .LessThanOrEqualTo(DateTime.UtcNow.Year + 1)
            .When(x => x.Year.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.YearMustBePositive));

        // Nếu có Month thì phải có Year (và ngược lại)
        RuleFor(x => x.Year)
            .NotNull()
            .When(x => x.Month.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.YearRequiredWithMonth));

        RuleFor(x => x.Month)
            .NotNull()
            .When(x => x.Year.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.MonthRequiredWithYear));
    }
}
