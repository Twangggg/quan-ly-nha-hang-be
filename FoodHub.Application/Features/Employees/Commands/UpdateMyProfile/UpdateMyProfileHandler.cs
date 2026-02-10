using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
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
        private readonly IMessageService _messageService;

        public UpdateProfileHandler(IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
        }
        public async Task<Result<UpdateProfileResponse>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<Employee>();

            var employee = await repo.Query()
                .FirstOrDefaultAsync(emp => emp.EmployeeId == request.EmployeeId, cancellationToken);

            if (employee == null)
            {
                return Result<UpdateProfileResponse>.NotFound(_messageService.GetMessage(MessageKeys.Employee.NotFound));
            }

            var fullName = request.FullName?.Trim() ?? string.Empty;
            var email = request.Email?.Trim().ToLower() ?? string.Empty;
            var phone = request.Phone?.Trim() ?? string.Empty;
            var address = request.Address?.Trim() ?? string.Empty;
            var dateOfBirth = request.DateOfBirth ?? default;

            // Check duplicate phone number
            var phoneExists = await repo.Query().AnyAsync(e => e.EmployeeId != request.EmployeeId && e.Phone == phone, cancellationToken);
            if (phoneExists)
            {
                return Result<UpdateProfileResponse>.Failure(_messageService.GetMessage(MessageKeys.Profile.PhoneExists));
            }

            // Check duplicate email
            var emailExists = await repo.Query().AnyAsync(e => e.EmployeeId != request.EmployeeId && e.Email == email, cancellationToken);
            if (emailExists)
            {
                return Result<UpdateProfileResponse>.Failure(_messageService.GetMessage(MessageKeys.Profile.EmailExists));
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
