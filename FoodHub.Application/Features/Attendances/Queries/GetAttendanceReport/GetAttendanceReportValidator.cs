using FluentValidation;

namespace FoodHub.Application.Features.Attendances.Queries.GetAttendanceReport
{
    public class GetAttendanceReportValidator : AbstractValidator<GetAttendanceReportQuery>
    {
        public GetAttendanceReportValidator()
        {
            RuleFor(x => x.Pagination)
                .NotNull()
                .WithMessage("Pagination parameters are required.");

            RuleFor(x => x.Pagination.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page number must be at least 1.");

            RuleFor(x => x.Pagination.PageSize)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page size must be at least 1.");

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate ?? DateOnly.MinValue)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("End date must be greater than or equal to start date.");
        }
    }
}
