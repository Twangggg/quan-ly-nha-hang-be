using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FluentValidation;

namespace FoodHub.Application.Features.Attendances.Queries.ExportAttendanceReport
{
    public class ExportAttendanceReportValidator : AbstractValidator<ExportAttendanceReportQuery>
    {
        public ExportAttendanceReportValidator(IMessageService messageService)
        {
            RuleFor(x => x.Pagination)
                .NotNull()
                .WithMessage(messageService.GetMessage(MessageKeys.Attendance.PaginationRequired));

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate ?? DateOnly.MinValue)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage(messageService.GetMessage(MessageKeys.Attendance.DateRangeInvalid));
        }
    }
}
