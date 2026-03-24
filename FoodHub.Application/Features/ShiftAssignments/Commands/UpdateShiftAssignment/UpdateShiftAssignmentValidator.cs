using FluentValidation;
using FoodHub.Application.Interfaces;

namespace FoodHub.Application.Features.ShiftAssignments.Commands.UpdateShiftAssignment
{
    public class UpdateShiftAssignmentValidator : AbstractValidator<UpdateShiftAssignmentCommand>
    {
        public UpdateShiftAssignmentValidator()
        {
            RuleFor(x => x.ShiftAssignmentId)
                .NotEmpty();

            RuleFor(x => x.ShiftId)
                .NotEmpty();

            RuleFor(x => x.Note)
                .MaximumLength(500)
                .When(x => x.Note != null);

            RuleFor(x => x.AssignedDate)
                .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
                .WithMessage("Ngày phân công không được trong quá khứ xa (trước 1 ngày so với hôm nay).");
        }
    }
}
