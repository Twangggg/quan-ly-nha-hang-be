using AutoMapper;
using FluentValidation;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Employees.Commands
{
    public class UpdateProfileResponse : IMapFrom<Employee>
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Address { get; set; }
        public DateOnly? DateOfBirth { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Address { get; set; }
        public DateOnly? DateOfBirth { get; set; }
    }
    public class UpdateProfileMapping : Profile
    {
        public UpdateProfileMapping()
        {
            CreateMap<UpdateProfileRequest, Employee>();
        }
    }
    public record UpdateMyProfileCommand(Guid EmployeeId, UpdateProfileRequest UpdateProfileRequest) : IRequest<UpdateProfileResponse>;
    public class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand, UpdateProfileResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UpdateMyProfileCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<UpdateProfileResponse> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<Employee>();

            var employee = await repo.Query()
                .FirstOrDefaultAsync(emp => emp.EmployeeId == request.EmployeeId, cancellationToken)
                ?? throw new NotFoundException("User not found");

            var phoneExists = await repo.Query()
            .AnyAsync(e => e.Phone == request.UpdateProfileRequest.Phone && e.EmployeeId != employee.EmployeeId, cancellationToken);

            var emailExists = await repo.Query()
                .AnyAsync(e => e.Email == request.UpdateProfileRequest.Email && e.EmployeeId != employee.EmployeeId, cancellationToken);

            if (phoneExists)
                throw new BusinessException("Số điện thoại đã tồn tại");
            if (emailExists)
                throw new BusinessException("Email da ton tai");

            employee.UpdatedAt = DateTime.UtcNow;

            _mapper.Map(request.UpdateProfileRequest, employee);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return _mapper.Map<UpdateProfileResponse>(employee);
        }
    }
    public class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
    {
        public UpdateMyProfileCommandValidator()
        {
            RuleFor(x => x.UpdateProfileRequest.FullName)
                .NotEmpty().WithMessage("Họ và tên không được để trống")
                .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự");

            RuleFor(x => x.UpdateProfileRequest.Email)
                .NotEmpty().WithMessage("Email không được để trống")
                .EmailAddress().WithMessage("Email không hợp lệ");

            RuleFor(x => x.UpdateProfileRequest.Phone)
                .NotEmpty().WithMessage("Số điện thoại không được để trống")
                .Matches(@"^(0|\+84)[3|5|7|8|9][0-9]{8}$")
                .WithMessage("Số điện thoại Việt Nam không hợp lệ");
        }
    }

}
