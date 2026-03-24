using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.ShiftAssignments.Commands.AutoAssignShift
{
    public class AutoAssignShiftValidator : AbstractValidator<AutoAssignShiftCommand>
    {
        public AutoAssignShiftValidator(IMessageService messageService)
        {
            RuleFor(x => x.EmployeeId)
                .NotEmpty();

            RuleFor(x => x.ShiftId)
                .NotEmpty();

            RuleFor(x => x.FromDate)
                .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
                .WithMessage("Ngày bắt đầu không được trong quá khứ xa.");

            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate)
                .WithMessage("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");

            RuleFor(x => x.Note)
                .MaximumLength(500)
                .When(x => x.Note != null);
        }
    }
}
