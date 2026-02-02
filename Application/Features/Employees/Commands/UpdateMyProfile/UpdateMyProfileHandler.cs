using AutoMapper;
using FoodHub.Application.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Employees.Commands.UpdateMyProfile
{
    public class Handler : IRequestHandler<Command, Result<Response>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        public Handler(IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
        }

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<Employee>();

            var fullName = request.FullName.Trim();
            var email = request.Email.Trim().ToLower();
            var phone = request.Phone.Trim();
            var address = request.Address?.Trim();

            var employee = await repo.Query()
                .FirstOrDefaultAsync(emp => emp.EmployeeId == request.EmployeeId, cancellationToken);

            if (employee == null)
            {
                return Result<Response>.NotFound(_messageService.GetMessage(MessageKeys.Employee.NotFound));
            }

            // Check duplicate phone number
            var phoneExists = await repo.Query().AnyAsync(e => e.EmployeeId != request.EmployeeId && e.Phone == phone, cancellationToken);
            if (phoneExists)
            {
                return Result<Response>.Failure(_messageService.GetMessage(MessageKeys.Profile.PhoneExists));
            }

            // Check duplicate email
            var emailExists = await repo.Query().AnyAsync(e => e.EmployeeId != request.EmployeeId && e.Email == email, cancellationToken);
            if (emailExists)
            {
                return Result<Response>.Failure(_messageService.GetMessage(MessageKeys.Profile.EmailExists));
            }

            // Update data
            employee.FullName = fullName;
            employee.Email = email;
            employee.Phone = phone;
            employee.Address = address;
            employee.DateOfBirth = request.DateOfBirth;
            employee.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = _mapper.Map<Response>(employee);
            return Result<Response>.Success(response);
        }
    }
}
