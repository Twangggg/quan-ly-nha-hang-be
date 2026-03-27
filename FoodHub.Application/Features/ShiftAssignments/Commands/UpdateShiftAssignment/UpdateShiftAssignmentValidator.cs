using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FluentValidation;

namespace FoodHub.Application.Features.ShiftAssignments.Commands.UpdateShiftAssignment
{
    public class UpdateShiftAssignmentValidator : AbstractValidator<UpdateShiftAssignmentCommand>
    {
        public UpdateShiftAssignmentValidator(IMessageService messageService)
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
                .WithMessage(messageService.GetMessage(MessageKeys.ShiftAssignment.DateInPast));
        }
    }
}
