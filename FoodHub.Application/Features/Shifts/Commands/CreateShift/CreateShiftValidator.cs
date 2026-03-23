using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Shifts.Commands.CreateShift
{
    public class CreateShiftValidator : AbstractValidator<CreateShiftCommand>
    {
        public CreateShiftValidator(IUnitOfWork unitOfWork, IMessageService messageService)
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
                    return !await unitOfWork.Repository<Shift>()
                        .Query()
                        .AnyAsync(s =>
                            s.StartTime == cmd.StartTime &&
                            s.EndTime == cmd.EndTime &&
                            s.Status == ShiftStatus.Active,
                            cancellation);
                })
                .WithMessage(messageService.GetMessage(MessageKeys.Shift.DuplicateTime));
        }
    }
}
