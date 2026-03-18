using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;

namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetDailyReport;

public class GetDailyReportQueryValidator : AbstractValidator<GetDailyReportQuery>
{
    public GetDailyReportQueryValidator(IMessageService messageService)
    {
        // Date không được ở tương lai (cho phép buffer 1 ngày để cover timezone VN +7)
        RuleFor(x => x.Date)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .When(x => x.Date.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.DateNotInFuture));

        // MovingAverageDays phải dương và không vượt quá 365 ngày
        RuleFor(x => x.MovingAverageDays)
            .GreaterThan(0)
            .When(x => x.MovingAverageDays != 0) // 0 là sentinel value trong Handler để skip moving avg
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.MovingAverageDaysMustBePositive));

        RuleFor(x => x.MovingAverageDays)
            .LessThanOrEqualTo(365)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.MovingAverageDaysMustNotExceed365));
    }
}
