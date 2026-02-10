using FluentValidation;
using FoodHub.Application.Constants;

namespace FoodHub.Application.Features.Employees.Commands.CreateEmployee
{
    public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeCommand>
    {
        public CreateEmployeeValidator(Interfaces.IUnitOfWork unitOfWork, Interfaces.IMessageService messageService)
        {
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty().EmailAddress()
                .MustAsync(async (email, cancellation) =>
                {
                    return !await unitOfWork.Repository<Domain.Entities.Employee>()
                        .AnyAsync(e => e.Email == email);
                }).WithMessage(messageService.GetMessage(MessageKeys.Profile.EmailExists));

            RuleFor(x => x.Role).IsInEnum();
        }
    }
}
