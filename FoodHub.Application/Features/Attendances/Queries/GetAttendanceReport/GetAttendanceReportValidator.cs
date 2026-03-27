using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FluentValidation;

namespace FoodHub.Application.Features.Attendances.Queries.GetAttendanceReport
{
    public class GetAttendanceReportValidator : AbstractValidator<GetAttendanceReportQuery>
    {
        public GetAttendanceReportValidator(IMessageService messageService)
        {
            RuleFor(x => x.Pagination)
                .NotNull()
                .WithMessage(messageService.GetMessage(MessageKeys.Attendance.PaginationRequired));

            RuleFor(x => x.Pagination.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage(messageService.GetMessage(MessageKeys.Attendance.PageNumberMin));

            RuleFor(x => x.Pagination.PageSize)
                .GreaterThanOrEqualTo(1)
                .WithMessage(messageService.GetMessage(MessageKeys.Attendance.PageSizeMin));

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate ?? DateOnly.MinValue)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage(messageService.GetMessage(MessageKeys.Attendance.DateRangeInvalid));
        }
    }
}
