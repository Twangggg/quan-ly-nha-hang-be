using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Authentication.Commands.Login;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FoodHub.Application.Constants;

namespace FoodHub.Application.Features.Authentication.Commands.RefreshToken
{
    public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IMessageService _messageService;

        public RefreshTokenHandler(IUnitOfWork unitOfWork, ITokenService tokenService, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _messageService = messageService;
        }

        public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var storedToken = await _unitOfWork.Repository<Domain.Entities.RefreshToken>()
                .Query()
                .Include(x => x.Employee) // Load Employee explicitly
                .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

            // Validation Checks
            if (storedToken == null)
            {
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.RefreshTokenNotFound));
            }

            if (storedToken.Expires < DateTime.UtcNow)
            {
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.RefreshTokenExpired));
            }

            if (storedToken.IsRevoked)
            {
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.RefreshTokenRevoked));
            }

            // Revoke current token (Token Rotation)
            storedToken.IsRevoked = true;
            storedToken.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Domain.Entities.RefreshToken>().Update(storedToken);

            // Generate new tokens
            var employee = storedToken.Employee;

            // Check if employee is still active
            if (employee.Status != EmployeeStatus.Active)
            {
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.AccountInactive));
            }

            var newAccessToken = _tokenService.GenerateAccessToken(employee);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // Logic: Inherit "Remember Me" status from the old token
            // Check if the old token had a duration significantly longer than the default (7 days)
            var defaultDays = _tokenService.GetRefreshTokenExpirationDays();
            var oldDurationDays = (storedToken.Expires - storedToken.CreatedAt).TotalDays;

            // If old usage was > default + 1 (allow small margin), treat as RememberMe (30 days)
            var isLongLived = oldDurationDays > (defaultDays + 1);
            var newDurationDays = isLongLived ? 30 : defaultDays;

            var newRefreshTokenEntity = new Domain.Entities.RefreshToken
            {
                Token = newRefreshToken,
                Expires = DateTime.UtcNow.AddDays(newDurationDays),
                EmployeeId = employee.EmployeeId
            };

            await _unitOfWork.Repository<Domain.Entities.RefreshToken>().AddAsync(newRefreshTokenEntity);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                EmployeeCode = employee.EmployeeCode,
                Email = employee.Email,
                Role = employee.Role.ToString(),
                RefreshTokenExpiresIn = (newRefreshTokenEntity.Expires - DateTime.UtcNow).TotalSeconds,
                ExpiresIn = _tokenService.GetTokenExpirationSeconds()
            };

            return Result<LoginResponse>.Success(response);
        }
    }
}
