using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.AuditLogs.Queries.GetAuditLogs
{
    public class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
    {
        public GetAuditLogsQueryValidator(IMessageService messageService)
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.PageNumberAtLeastOne));

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.PageSizeBetween));

            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate)
                .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
                .WithMessage(messageService.GetMessage(MessageKeys.Common.ToDateAfterFromDate));
        }
    }
}
