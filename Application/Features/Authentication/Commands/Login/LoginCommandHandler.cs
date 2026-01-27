using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Authentication;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
    {
        private readonly IGenericRepository<Employee> _employeeRepo;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public LoginCommandHandler(
            IGenericRepository<Employee> employeeRepo,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            IMapper mapper)
        {
            _employeeRepo = employeeRepo;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        public async Task<Result<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // Tìm employee bằng Username hoặc EmployeeCode
            var employee = await _employeeRepo
                .Query()
                .FirstOrDefaultAsync(e => e.Username == request.Username ||
                                         e.EmployeeCode == request.Username, cancellationToken);

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

            // Map thông tin employee
            var employeeInfo = _mapper.Map<EmployeeInfoDto>(employee);

            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                User = employeeInfo
            };

            return Result<LoginResponseDto>.Success(response);
        }
    }
}
