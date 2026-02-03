using AutoMapper;
using FoodHub.Application.Constants;
using FluentValidation;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Employees.Commands.UpdateMyProfile
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator(IMessageService messageService)
        {
            RuleFor(x => x.EmployeeId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Profile.EmployeeIdRequired));

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Profile.FullNameRequired))
                .MaximumLength(100).WithMessage(messageService.GetMessage(MessageKeys.Profile.FullNameMaxLength));

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Profile.EmailRequired))
                .EmailAddress().WithMessage(messageService.GetMessage(MessageKeys.Profile.EmailInvalid));

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Profile.PhoneRequired))
                .Matches(@"^(0|\+84)[3|5|7|8|9][0-9]{8}$")
                .WithMessage(messageService.GetMessage(MessageKeys.Profile.PhoneInvalid));
        }
    }

}
