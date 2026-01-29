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
    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public Handler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            // setup
            var repo = _unitOfWork.Repository<Employee>();

            var fullName = request.FullName.Trim();
            var email = request.Email.Trim().ToLower();
            var phone = request.Phone.Trim();
            var address = request.Address?.Trim();

            var employee = await repo.Query()
                .FirstOrDefaultAsync(emp => emp.EmployeeId == request.EmployeeId, cancellationToken)
                ?? throw new NotFoundException("User not found");

            var employees = await repo.Query()
                .Where(e => e.EmployeeId != request.EmployeeId && (e.Email == email || e.Phone == phone))
                .ToListAsync(cancellationToken);
            // setup

            // Check duplicate phone number
            var phoneExists = await repo.Query().AnyAsync(e => e.EmployeeId != request.EmployeeId && e.Phone == phone, cancellationToken);
            if (phoneExists)
                throw new BusinessException("Phone number already exists");

            // Check duplicate email
            var emailExists = await repo.Query().AnyAsync(e => e.EmployeeId != request.EmployeeId && e.Email == email, cancellationToken);
            if (emailExists)
                throw new BusinessException("Email already exists");

            // Update data
            employee.FullName = fullName;
            employee.Email = email;
            employee.Phone = phone;
            employee.Address = address;
            employee.DateOfBirth = request.DateOfBirth;
            employee.UpdatedAt = DateTime.UtcNow;
            // Update data
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return _mapper.Map<Response>(employee);
        }
    }
}
