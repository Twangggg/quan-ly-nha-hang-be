using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.KDS.Queries.GetKdsAuditLogs;

public class GetKdsAuditLogsQueryValidator : AbstractValidator<GetKdsAuditLogsQuery>
{
    public GetKdsAuditLogsQueryValidator(IMessageService messageService)
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage(messageService.GetMessage(MessageKeys.Common.PageNumberAtLeastOne));

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage(messageService.GetMessage(MessageKeys.Common.PageSizeBetween));

        RuleFor(x => x.Station)
            .MaximumLength(50)
            .When(x => x.Station != null)
            .WithMessage(messageService.GetMessage(MessageKeys.KDS.StationMaxLength));

        RuleFor(x => x.Action)
            .MaximumLength(50)
            .When(x => x.Action != null)
            .WithMessage(messageService.GetMessage(MessageKeys.KDS.ActionMaxLength));

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.Common.ToDateAfterFromDate));

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.FromDate.HasValue)
            .WithMessage(messageService.GetMessage(MessageKeys.Common.DateNotInFuture));
    }
}
