using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Employees.Commands.UpdateMyProfile
{
    public class UpdateProfileHandler : IRequestHandler<UpdateProfileCommand, Result<UpdateProfileResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UpdateProfileHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<Result<UpdateProfileResponse>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<Employee>();

            var employee = await repo.Query()
                .FirstOrDefaultAsync(emp => emp.EmployeeCode == request.EmployeeCode, cancellationToken);

            var fullName = request.FullName.Trim();
            var email = request.Email.Trim().ToLower();
            var phone = request.Phone.Trim();
            var address = request.Address.Trim();
            var dateOfBirth = request.DateOfBirth;

            if (employee == null)
            {
                return Result<UpdateProfileResponse>.NotFound("User not found.");
            }

            // Check duplicate phone number
            var phoneExists = await repo.Query().AnyAsync(e => e.EmployeeCode != request.EmployeeCode && e.Phone == phone, cancellationToken);
            if (phoneExists)
            {
                return Result<UpdateProfileResponse>.Failure("Phone number already exists.");
            }

            // Check duplicate email
            var emailExists = await repo.Query().AnyAsync(e => e.EmployeeCode != request.EmployeeCode && e.Email == email, cancellationToken);
            if (emailExists)
            {
                return Result<UpdateProfileResponse>.Failure("Email already exists.");
            }

            // Update data
            employee.FullName = fullName;
            employee.Email = email;
            employee.Phone = phone;
            employee.Address = address;
            employee.DateOfBirth = request.DateOfBirth;
            employee.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = _mapper.Map<UpdateProfileResponse>(employee);
            return Result<UpdateProfileResponse>.Success(response);
        }
    }
}
