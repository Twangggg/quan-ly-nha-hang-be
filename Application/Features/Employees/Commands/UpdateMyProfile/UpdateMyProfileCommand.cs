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
    public record Command(
        Guid EmployeeId,
        string FullName,
        string Email,
        string Phone,
        string? Address,
        DateOnly? DateOfBirth
        ) : IRequest<Response>;
}
