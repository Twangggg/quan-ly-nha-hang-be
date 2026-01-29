using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Authentication;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
<<<<<<< HEAD
=======
using FoodHub.Domain.Interfaces;
>>>>>>> origin/feature/profile-nhudm
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
    {
<<<<<<< HEAD
        private readonly IUnitOfWork _unitOfWork;
=======
        private readonly IGenericRepository<Employee> _employeeRepo;
>>>>>>> origin/feature/profile-nhudm
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public LoginCommandHandler(
<<<<<<< HEAD
            IUnitOfWork unitOfWork,
=======
            IGenericRepository<Employee> employeeRepo,
>>>>>>> origin/feature/profile-nhudm
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            IMapper mapper)
        {
<<<<<<< HEAD
            _unitOfWork = unitOfWork;
=======
            _employeeRepo = employeeRepo;
>>>>>>> origin/feature/profile-nhudm
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        public async Task<Result<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
<<<<<<< HEAD
            // Tìm employee chỉ bằng EmployeeCode
            var employee = await _unitOfWork.Repository<Employee>()
                .Query()
                .FirstOrDefaultAsync(e => e.EmployeeCode == request.EmployeeCode, cancellationToken);
=======
            // Tìm employee bằng Username hoặc EmployeeCode
            var employee = await _employeeRepo
                .Query()
                .FirstOrDefaultAsync(e => e.Username == request.Username ||
                                         e.EmployeeCode == request.Username, cancellationToken);
>>>>>>> origin/feature/profile-nhudm

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

<<<<<<< HEAD
            // Tạo access token
            var accessToken = _tokenService.GenerateAccessToken(employee);
            var expiresIn = _tokenService.GetTokenExpirationSeconds();

            // Tạo refresh token
            var refreshToken = _tokenService.GenerateRefreshToken();
            // Config: Default 7 days from appsettings
            var configDays = _tokenService.GetRefreshTokenExpirationDays(); 
            
            // Logic: If RememberMe -> 30 Days (or config * 4). If not -> Config (7 days).
            // OR: If not RememberMe -> Session duration?
            // Let's go with:
            // RememberMe = True -> 30 Days.
            // RememberMe = False -> 7 Days.
            var expirationDays = request.RememberMe ? 30 : configDays;
            var expirationDate = DateTime.UtcNow.AddDays(expirationDays);

            var refreshTokenEntity = new FoodHub.Domain.Entities.RefreshToken
            {
                Token = refreshToken,
                Expires = expirationDate,

                EmployeeId = employee.EmployeeId
            };

            await _unitOfWork.Repository<FoodHub.Domain.Entities.RefreshToken>().AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
=======
            // Tạo token
            var accessToken = _tokenService.GenerateAccessToken(employee);

            // Map thông tin employee
            var employeeInfo = _mapper.Map<EmployeeInfoDto>(employee);
>>>>>>> origin/feature/profile-nhudm

            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
<<<<<<< HEAD
                RefreshToken = refreshToken,
                RefreshTokenExpiresIn = (expirationDate - DateTime.UtcNow).TotalSeconds,
                EmployeeCode = employee.EmployeeCode,
                Email = employee.Email,
                Role = employee.Role.ToString(),
                ExpiresIn = expiresIn
=======
                TokenType = "Bearer",
                User = employeeInfo
>>>>>>> origin/feature/profile-nhudm
            };

            return Result<LoginResponseDto>.Success(response);
        }
    }
}
