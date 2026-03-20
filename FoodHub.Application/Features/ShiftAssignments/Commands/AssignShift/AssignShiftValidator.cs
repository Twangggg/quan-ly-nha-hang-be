using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.ShiftAssignments.Commands.AssignShift
{
    public class AssignShiftValidator : AbstractValidator<AssignShiftCommand>
    {
        public AssignShiftValidator(IUnitOfWork unitOfWork, IMessageService messageService)
        {
            RuleFor(x => x.EmployeeId)
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
