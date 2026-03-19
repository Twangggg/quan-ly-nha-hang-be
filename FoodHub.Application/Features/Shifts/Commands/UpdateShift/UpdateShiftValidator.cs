using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Shifts.Commands.UpdateShift
{
    public class UpdateShiftValidator : AbstractValidator<UpdateShiftCommand>
    {
        public UpdateShiftValidator(IUnitOfWork unitOfWork, IMessageService messageService)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithMessage(messageService.GetMessage(MessageKeys.Shift.NameRequired))
                .MaximumLength(100);

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime)
                    .WithMessage(messageService.GetMessage(MessageKeys.Shift.InvalidTime));

            RuleFor(x => x)
                .MustAsync(async (cmd, cancellation) =>
                {
                    // Check for duplicate, excluding the shift being updated
                    return !await unitOfWork.Repository<Shift>()
                        .Query()
                        .AnyAsync(s =>
                            s.ShiftId != cmd.ShiftId &&
                            s.StartTime == cmd.StartTime &&
                            s.EndTime == cmd.EndTime &&
                            s.Status == ShiftStatus.Active,
                            cancellation);
                })
                .WithMessage(messageService.GetMessage(MessageKeys.Shift.DuplicateTime));
        }
    }
}
