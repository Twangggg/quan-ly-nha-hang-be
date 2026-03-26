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
        }
    }
}
