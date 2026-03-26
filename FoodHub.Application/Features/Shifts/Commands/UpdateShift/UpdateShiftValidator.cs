using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
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
                .Must(x => x.StartTime >= new TimeSpan(10, 30, 0) && x.EndTime <= new TimeSpan(23, 0, 0))
                .WithMessage(messageService.GetMessage(MessageKeys.Shift.OutsideOperatingHours))
                .Must(x => !(x.StartTime < new TimeSpan(17, 0, 0) && x.EndTime > new TimeSpan(14, 0, 0)))
                .WithMessage(messageService.GetMessage(MessageKeys.Shift.DuringBreakTime));

            RuleFor(x => x)
                .MustAsync(async (cmd, cancellation) =>
                {
                    // Check for duplicate, excluding the shift being updated
                    return !await unitOfWork.Repository<Shift>()
                        .Query()
                        .AnyAsync(s =>
                            s.ShiftId != cmd.ShiftId &&
                            s.StartTime < cmd.EndTime &&
                            s.EndTime > cmd.StartTime &&
                            s.Status == ShiftStatus.Active,
                            cancellation);
                })
                .WithMessage(messageService.GetMessage(MessageKeys.Shift.DuplicateTime));
        }
    }
}
