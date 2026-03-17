using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Employees.Commands.ResetEmployeePassword
{
    public class ResetEmployeePasswordHandler
        : IRequestHandler<ResetEmployeePasswordCommand, Result<ResetEmployeePasswordResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public ResetEmployeePasswordHandler(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            IBackgroundEmailSender emailSender,
            ICurrentUserService currentUserService,
            IMessageService messageService
        )
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _emailSender = emailSender;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<ResetEmployeePasswordResponse>> Handle(
            ResetEmployeePasswordCommand request,
            CancellationToken cancellationToken
        )
        {
            var managerId = _currentUserService.UserId;

            if (string.IsNullOrEmpty(managerId) || !Guid.TryParse(managerId, out var managerGuid))
            {
                return Result<ResetEmployeePasswordResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Employee.CannotIdentifyManager)
                );
            }

            var manager = await _unitOfWork.Repository<Employee>().GetByIdAsync(managerGuid);

            if (manager == null || manager.Role != EmployeeRole.Manager)
            {
                return Result<ResetEmployeePasswordResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.ResetPassword.OnlyManagerCanReset)
                );
            }

            var employee = await _unitOfWork
                .Repository<Employee>()
                .GetByIdAsync(request.EmployeeId);

            if (employee == null)
            {
                return Result<ResetEmployeePasswordResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Employee.NotFound)
                );
            }

            if (employee.Status != EmployeeStatus.Active)
            {
                return Result<ResetEmployeePasswordResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.ResetPassword.OnlyActiveEmployeeCanReset)
                );
            }

            var newPassword = string.IsNullOrEmpty(request.NewPassword)
                ? _passwordService.GenerateRandomPassword()
                : request.NewPassword;

            employee.ResetPassword(_passwordService.HashPassword(newPassword), managerGuid);

            var refreshTokens = await _unitOfWork
                .Repository<RefreshToken>()
                .Query()
                .Where(rt => rt.EmployeeId == employee.EmployeeId && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in refreshTokens)
            {
                token.Revoke();
            }

            employee.ResetPassword(_passwordService.HashPassword(newPassword), managerGuid);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _emailSender.EnqueuePasswordResetByManagerEmailAsync(
                employee.Email,
                employee.FullName,
                employee.EmployeeCode,
                newPassword,
                manager.FullName,
                employee.EmployeeId,
                managerGuid,
                cancellationToken
            );

            var response = new ResetEmployeePasswordResponse
            {
                EmployeeId = employee.EmployeeId,
                EmployeeCode = employee.EmployeeCode,
                FullName = employee.FullName,
                Email = employee.Email,
                NewPassword = newPassword,
                ResetAt = DateTime.UtcNow,
                Message = _messageService.GetMessage(
                    MessageKeys.ResetPassword.SuccessWithEmail,
                    employee.Email
                ),
            };

            return Result<ResetEmployeePasswordResponse>.Success(response);
        }
    }
}
