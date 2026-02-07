using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Authentication.Commands.ChangePassword
{
    public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IRateLimiter _rateLimiter;
        private readonly IMessageService _messageService;

        public ChangePasswordHandler(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            ICurrentUserService currentUserService,
            IRateLimiter rateLimiter,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _currentUserService = currentUserService;
            _rateLimiter = rateLimiter;
            _messageService = messageService;
        }

        public async Task<Result<string>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn));
            }

            var employee = await _unitOfWork.Repository<Employee>()
                .Query()
                .FirstOrDefaultAsync(u => u.EmployeeId.ToString() == userId, cancellationToken);

            if (employee == null)
            {
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Auth.InvalidAction));
            }

            if (employee.Status != EmployeeStatus.Active)
            {
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Auth.InvalidAction));
            }

            var key = $"cp:{userId}";

            // Check if user is blocked
            if (await _rateLimiter.IsBlockedAsync(key, cancellationToken))
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Auth.TooManyAttempts));

            // Verify current password
            if (!_passwordService.VerifyPassword(request.CurrentPassword, employee.PasswordHash))
            {
                await RegisterFailedAttempt(key, cancellationToken);
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Password.IncorrectCurrent));
            }

            // All validations passed - reset fail count
            await _rateLimiter.ResetAsync(key, cancellationToken);

            // Update password
            employee.PasswordHash = _passwordService.HashPassword(request.NewPassword);
            employee.UpdatedAt = DateTime.UtcNow;

            //Revoke existing refresh tokens
            var refreshTokens = await _unitOfWork.Repository<Domain.Entities.RefreshToken>()
                .Query()
                .Where(rt => rt.EmployeeId == employee.EmployeeId && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<Domain.Entities.RefreshToken>().Update(token);
            }

            //Log password reset
            await _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
            {
                LogId = Guid.NewGuid(),
                TargetId = employee.EmployeeId,
                PerformedByEmployeeId = employee.EmployeeId, // self-change
                Reason = "SelfChange",
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<string>.Success(_messageService.GetMessage(MessageKeys.Auth.PasswordChangedSuccess));
        }

        private async Task RegisterFailedAttempt(string key, CancellationToken cancellationToken)
        {
            await _rateLimiter.RegisterFailAsync(
                key,
                limit: 5,
                window: TimeSpan.FromMinutes(15),
                blockFor: TimeSpan.FromMinutes(15),
                cancellationToken);
        }


    }
}
