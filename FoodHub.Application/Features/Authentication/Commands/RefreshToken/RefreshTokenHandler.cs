using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Authentication.Commands.Login;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
            var hashedRefreshToken = Domain.Entities.RefreshToken.HashToken(request.RefreshToken);
            var storedToken = await _unitOfWork.Repository<Domain.Entities.RefreshToken>()
                .Query()
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(
                    x => x.Token == hashedRefreshToken || x.Token == request.RefreshToken,
                    cancellationToken
                );

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

            var defaultDays = _tokenService.GetRefreshTokenExpirationDays();
            var oldDurationDays = (storedToken.Expires - storedToken.CreatedAt).TotalDays;
            var isLongLived = oldDurationDays > (defaultDays + 1);
            var newDurationDays = isLongLived ? 30 : defaultDays;

            var newRefreshTokenEntity = Domain.Entities.RefreshToken.CreateWithDays(
                employee.EmployeeId,
                newRefreshToken,
                newDurationDays
            );

            await _unitOfWork.Repository<Domain.Entities.RefreshToken>().AddAsync(newRefreshTokenEntity);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new LoginResponse
            {
                EmployeeId = employee.EmployeeId,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                EmployeeCode = employee.EmployeeCode,
                Username = employee.Username,
                FullName = employee.FullName,
                Email = employee.Email,
                Role = employee.Role.ToString(),
                RefreshTokenExpiresIn = (newRefreshTokenEntity.Expires - DateTime.UtcNow).TotalSeconds,
                ExpiresIn = _tokenService.GetTokenExpirationSeconds()
            };

            return Result<LoginResponse>.Success(response);
        }
    }
}
