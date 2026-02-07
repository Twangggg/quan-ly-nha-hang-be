using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FoodHub.Application.Constants;

namespace FoodHub.Application.Features.Authentication.Commands.Login
{
    public class LoginHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;
        private readonly IRateLimiter _rateLimiter;
        private readonly IMessageService _messageService;

        public LoginHandler(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            ITokenService tokenService,
            IRateLimiter rateLimiter,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _tokenService = tokenService;
            _rateLimiter = rateLimiter;
            _messageService = messageService;
        }

        public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var rateLimitKey = $"login_attempt:{request.EmployeeCode}";

            // Check if user is currently blocked
            if (await _rateLimiter.IsBlockedAsync(rateLimitKey, cancellationToken))
            {
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.AccountBlocked));
            }

            // Tìm employee chỉ bằng EmployeeCode
            var employee = await _unitOfWork.Repository<Employee>()
                .Query()
                .FirstOrDefaultAsync(e => e.EmployeeCode == request.EmployeeCode, cancellationToken);

            if (employee == null)
            {
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.InvalidCredentials));
            }

            // Kiểm tra mật khẩu
            if (!_passwordService.VerifyPassword(request.Password, employee.PasswordHash))
            {
                // Register failed attempt (block after 5 attempts in 15 mins)
                await _rateLimiter.RegisterFailAsync(
                    rateLimitKey,
                    limit: 5,
                    window: TimeSpan.FromMinutes(15),
                    blockFor: TimeSpan.FromMinutes(15),
                    cancellationToken);

                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.InvalidCredentials));
            }

            // All good - reset failure count
            await _rateLimiter.ResetAsync(rateLimitKey, cancellationToken);

            // Kiểm tra trạng thái account
            if (employee.Status == EmployeeStatus.Inactive)
            {
                return Result<LoginResponse>.Failure(_messageService.GetMessage(MessageKeys.Auth.AccountInactive));
            }

            // Tạo access token
            var accessToken = _tokenService.GenerateAccessToken(employee);
            var expiresIn = _tokenService.GetTokenExpirationSeconds();

            // Tạo refresh token
            var refreshToken = _tokenService.GenerateRefreshToken();
            var configDays = _tokenService.GetRefreshTokenExpirationDays();

            // Logic: If RememberMe -> 30 Days (or config * 4). If not -> Config (7 days).
            var expirationDays = request.RememberMe ? 30 : configDays;
            var expirationDate = DateTime.UtcNow.AddDays(expirationDays);

            var refreshTokenEntity = new Domain.Entities.RefreshToken
            {
                Token = refreshToken,
                Expires = expirationDate,

                EmployeeId = employee.EmployeeId
            };

            await _unitOfWork.Repository<FoodHub.Domain.Entities.RefreshToken>().AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenExpiresIn = (expirationDate - DateTime.UtcNow).TotalSeconds,
                EmployeeCode = employee.EmployeeCode,
                Email = employee.Email,
                Role = employee.Role.ToString(),
                ExpiresIn = expiresIn
            };

            return Result<LoginResponse>.Success(response);
        }
    }
}
