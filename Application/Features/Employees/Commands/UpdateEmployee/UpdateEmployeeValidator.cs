using FluentValidation;
using FoodHub.Application.Constants;

namespace FoodHub.Application.Features.Employees.Commands.UpdateEmployee
{
    public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeCommand>
    {
        public UpdateEmployeeValidator(Interfaces.IUnitOfWork unitOfWork, Interfaces.IMessageService messageService)
        {
            RuleFor(x => x.EmployeeId).NotEmpty();

            RuleFor(x => x.Username)
                .MaximumLength(50)
                .MustAsync(async (command, username, cancellation) =>
                {
                    if (string.IsNullOrEmpty(username)) return true;
                    return !await unitOfWork.Repository<Domain.Entities.Employee>()
                        .AnyAsync(e => e.Username == username && e.EmployeeId != command.EmployeeId);
                }).WithMessage(messageService.GetMessage(MessageKeys.Profile.UsernameExists));

            RuleFor(x => x.Phone)
                .MaximumLength(15)
                .MustAsync(async (command, phone, cancellation) =>
                {
                    if (string.IsNullOrEmpty(phone)) return true;
                    return !await unitOfWork.Repository<Domain.Entities.Employee>()
                        .AnyAsync(e => e.Phone == phone && e.EmployeeId != command.EmployeeId);
                }).WithMessage(messageService.GetMessage(MessageKeys.Profile.PhoneExists));


            RuleFor(x => x.FullName)
                .NotEmpty()
                .When(x => x.FullName != null);

            RuleFor(x => x.Address).MaximumLength(255);
        }
    }
}
