using FluentValidation;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;

namespace FoodHub.Application.Features.Employees.Commands.UpdateMyProfile
{
    public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
    {
        public UpdateProfileValidator(Interfaces.Common.IMessageService messageService)
        {
            RuleFor(x => x.EmployeeId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Profile.FullNameRequired))
                .MaximumLength(100).WithMessage(messageService.GetMessage(MessageKeys.Profile.FullNameMaxLength));

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Profile.EmailRequired))
                .EmailAddress().WithMessage(messageService.GetMessage(MessageKeys.Profile.EmailInvalid));

            RuleFor(x => x.Phone)
                .Matches(@"^(0|84|\+84)(3|5|7|8|9)([0-9]{8})$")
                .WithMessage(messageService.GetMessage(MessageKeys.Profile.PhoneInvalid))
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.Address)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Profile.AddressRequired))
                .MaximumLength(200).WithMessage(messageService.GetMessage(MessageKeys.Profile.AddressMaxLength));

            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Profile.DateOfBirthRequired))
                .LessThan(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage(messageService.GetMessage(MessageKeys.Profile.DateOfBirthMustBePast));
        }
    }

}
