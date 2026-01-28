using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Authentication;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public LoginCommandHandler(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        public async Task<Result<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // Tìm employee chỉ bằng EmployeeCode
            var employee = await _unitOfWork.Repository<Employee>()
                .Query()
                .FirstOrDefaultAsync(e => e.EmployeeCode == request.EmployeeCode, cancellationToken);

            if (employee == null)
            {
                return Result<LoginResponseDto>.Failure(Messages.InvalidCredentials);
            }

            // Kiểm tra mật khẩu
            if (!_passwordHasher.VerifyPassword(request.Password, employee.PasswordHash))
            {
                return Result<LoginResponseDto>.Failure(Messages.InvalidCredentials);
            }

            // Kiểm tra trạng thái account
            if (employee.Status == EmployeeStatus.Inactive)
            {
                return Result<LoginResponseDto>.Failure(Messages.AccountInactive);
            }

            // Tạo token
            var accessToken = _tokenService.GenerateAccessToken(employee);
            var expiresIn = _tokenService.GetTokenExpirationSeconds();

            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
                EmployeeCode = employee.EmployeeCode,
                Email = employee.Email,
                ExpiresIn = expiresIn
            };

            return Result<LoginResponseDto>.Success(response);
        }
    }
}
