using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Employees.Commands.UpdateMyProfile
{
    public class UpdateProfileHandler
        : IRequestHandler<UpdateProfileCommand, Result<UpdateProfileResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateProfileHandler> _logger;

        public UpdateProfileHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ICacheService cacheService,
            ICurrentUserService currentUserService,
            ILogger<UpdateProfileHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _cacheService = cacheService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<UpdateProfileResponse>> Handle(
            UpdateProfileCommand request,
            CancellationToken cancellationToken
        )
        {
            if (
                !Guid.TryParse(_currentUserService.UserId, out var currentUserId)
                || currentUserId != request.EmployeeId
            )
            {
                _logger.LogWarning(
                    "Unauthorized profile update attempt for EmployeeId {EmployeeId} by UserId {UserId}",
                    request.EmployeeId,
                    _currentUserService.UserId
                );
                return Result<UpdateProfileResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Auth.UserNotLoggedIn),
                    ResultErrorType.Forbidden
                );
            }

            var repo = _unitOfWork.Repository<Employee>();

            var employee = await repo.Query()
                .FirstOrDefaultAsync(
                    emp => emp.EmployeeId == request.EmployeeId,
                    cancellationToken
                );

            if (employee == null)
            {
                return Result<UpdateProfileResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Employee.NotFound)
                );
            }

            var fullName = request.FullName?.Trim() ?? string.Empty;
            var email = request.Email?.Trim().ToLower() ?? string.Empty;
            var phone = string.IsNullOrWhiteSpace(request.Phone)
                ? employee.Phone
                : request.Phone.Trim();
            var address = request.Address?.Trim() ?? string.Empty;
            // Check duplicate phone number only when the client supplies a new phone value.
            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                var phoneExists = await repo.Query()
                    .AnyAsync(
                        e => e.EmployeeId != request.EmployeeId && e.Phone == phone,
                        cancellationToken
                    );
                if (phoneExists)
                {
                    return Result<UpdateProfileResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Profile.PhoneExists)
                    );
                }
            }

            // Check duplicate email
            var emailExists = await repo.Query()
                .AnyAsync(
                    e => e.EmployeeId != request.EmployeeId && e.Email == email,
                    cancellationToken
                );
            if (emailExists)
            {
                return Result<UpdateProfileResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Profile.EmailExists)
                );
            }

            employee.UpdateProfile(
                fullName,
                email,
                phone,
                address,
                request.DateOfBirth,
                currentUserId
            );

            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _cacheService.RemoveByPatternAsync("employee:list", cancellationToken);

            var response = _mapper.Map<UpdateProfileResponse>(employee);

            _logger.LogInformation(
                "Successfully updated profile for EmployeeId {EmployeeId}",
                employee.EmployeeId
            );

            return Result<UpdateProfileResponse>.Success(response);
        }
    }
}
