using AutoMapper;
using FluentValidation;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Employees.Commands.UpdateMyProfile
{
    public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
    {
        public UpdateProfileValidator()
        {
            RuleFor(x => x.EmployeeId)
                .NotEmpty().WithMessage("EmployeeId not empty");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name not empty")
                .MaximumLength(100).WithMessage("Full name not exceed 100 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email not empty")
                .EmailAddress().WithMessage("Email invalid");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Phone number not empty")
                .Matches(@"^(0|\+84)[3|5|7|8|9][0-9]{8}$")
                .WithMessage("Phone number invalid");

            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Address not empty")
                .MaximumLength(200).WithMessage("Address not exceed 200 characters");

            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage("Date of birth not empty")
                .LessThan(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Date of birth must be in the past");
        }
    }

}
