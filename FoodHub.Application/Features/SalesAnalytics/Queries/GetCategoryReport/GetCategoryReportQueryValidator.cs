using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;

namespace FoodHub.Application.Features.SalesAnalytics.Queries.GetCategoryReport;

public class GetCategoryReportQueryValidator : AbstractValidator<GetCategoryReportQuery>
{
    public GetCategoryReportQueryValidator(IMessageService messageService)
    {
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

        // Nếu có EndDate thì phải có StartDate (và ngược lại) để tránh open-ended range mơ hồ
        RuleFor(x => x.StartDate)
            .NotNull()
            .When(x => x.EndDate.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.StartDateRequiredWithEndDate));

        RuleFor(x => x.EndDate)
            .NotNull()
            .When(x => x.StartDate.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.SalesAnalytics.EndDateRequiredWithStartDate));
    }
}
