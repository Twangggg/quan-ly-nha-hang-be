using AutoMapper;
using FluentValidation;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Employees.Commands.UpdateMyProfile
{
    public record Command(
        Guid EmployeeId,
        string FullName,
        string Email,
        string Phone,
        string? Address,
        DateOnly? DateOfBirth
        ) : IRequest<Response>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.EmployeeId)
                .NotEmpty().WithMessage("EmployeeId không được để trống");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ và tên không được để trống")
                .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống")
                .EmailAddress().WithMessage("Email không hợp lệ");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Số điện thoại không được để trống")
                .Matches(@"^(0|\+84)[3|5|7|8|9][0-9]{8}$")
                .WithMessage("Số điện thoại Việt Nam không hợp lệ");
        }
    }

    public class Response : IMapFrom<Employee>
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Address { get; set; }
        public DateOnly? DateOfBirth { get; set; }
    }
    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public Handler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            // setup
            var repo = _unitOfWork.Repository<Employee>();

            var employee = await repo.Query()
                .FirstOrDefaultAsync(emp => emp.EmployeeId == request.EmployeeId, cancellationToken)
                ?? throw new NotFoundException("User not found");

            var employees = await repo.Query()
                .Where(e => e.EmployeeId != request.EmployeeId && (e.Email == request.Email || e.Phone == request.Phone))
                .ToListAsync(cancellationToken);
            // setup

            // Check duplicate phone number
            var phoneExists = employees.Any(e => e.Phone == request.Phone);
            if (phoneExists)
                throw new BusinessException("Số điện thoại đã tồn tại");

            // Check duplicate email
            var emailExists = employees.Any(e => e.Email == request.Email);
            if (emailExists)
                throw new BusinessException("Email da ton tai");

            // Update data
            employee.FullName = request.FullName;
            employee.Email = request.Email;
            employee.Phone = request.Phone;
            employee.Address = request.Address;
            employee.DateOfBirth = request.DateOfBirth;
            employee.UpdatedAt = DateTime.UtcNow;
            // Update data

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return _mapper.Map<Response>(employee);
        }
    }
}
