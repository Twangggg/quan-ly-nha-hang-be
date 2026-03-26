using FluentValidation;

namespace FoodHub.Application.Features.Attendances.Queries.ExportAttendanceReport
{
    public class ExportAttendanceReportValidator : AbstractValidator<ExportAttendanceReportQuery>
    {
        public ExportAttendanceReportValidator()
        {
            RuleFor(x => x.Pagination)
                .NotNull()
                .WithMessage("Pagination parameters are required.");

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate ?? DateOnly.MinValue)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("End date must be greater than or equal to start date.");
        }
    }
}
