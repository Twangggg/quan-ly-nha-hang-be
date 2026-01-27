using AutoMapper;
using FoodHub.Application.DTOs.Employees;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Employees.Query
{
    public record UpdateMyProfileQuery(Guid EmployeeId, UpdateProfileDto UpdateProfileDto) : IRequest<EmployeeDto>;
    public class UpdateMyProfileQueryHandler : IRequestHandler<UpdateMyProfileQuery, EmployeeDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UpdateMyProfileQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<EmployeeDto> Handle(UpdateMyProfileQuery request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<Employee>();

            var employee = await repo.Query()
                .FirstOrDefaultAsync(emp => emp.EmployeeId == request.EmployeeId, cancellationToken);

            if (employee == null) throw new Exception("User not found");

            var phoneExists = await repo.Query()
            .AnyAsync(e => e.Phone == request.UpdateProfileDto.Phone && e.EmployeeId != employee.EmployeeId, cancellationToken);

            if (phoneExists)
                throw new Exception("Số điện thoại đã tồn tại");

            //_mapper.Map(request.UpdateProfileDto, employee);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return _mapper.Map<EmployeeDto>(employee);
        }
    }
}
