using FluentValidation;

namespace FoodHub.Application.Features.KDS.Queries.GetKdsAuditLogs;

public class GetKdsAuditLogsQueryValidator : AbstractValidator<GetKdsAuditLogsQuery>
{
    public GetKdsAuditLogsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be greater than or equal to 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.Station)
            .MaximumLength(50)
            .When(x => x.Station != null)
            .WithMessage("Station name cannot exceed 50 characters");

        RuleFor(x => x.Action)
            .MaximumLength(50)
            .When(x => x.Action != null)
            .WithMessage("Action name cannot exceed 50 characters");

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("ToDate must be greater than or equal to FromDate");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.FromDate.HasValue)
            .WithMessage("FromDate cannot be in the future");
    }
}
