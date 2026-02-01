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
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
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
        }
    }

}
