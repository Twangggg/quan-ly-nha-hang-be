using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Shifts.Queries.GetShiftsByEmployeeId
{
    public class GetShiftsByEmployeeIdHandler : IRequestHandler<GetShiftsByEmployeeIdQuery, Result<List<GetShiftsByEmployeeIdResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public GetShiftsByEmployeeIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<Result<List<GetShiftsByEmployeeIdResponse>>> Handle(GetShiftsByEmployeeIdQuery request, CancellationToken cancellationToken)
        {
            var auditorId = _currentUserService.GetRequiredUserIdAsGuid();

            var shiftRepository = _unitOfWork.Repository<Shift>();

            var shifts = await shiftRepository
                .Query()
                .Include(s => s.ShiftAssignments)
                .Where(s => s.ShiftAssignments.Any(sa => sa.EmployeeId == auditorId))
                .ToListAsync(cancellationToken);

            var response = _mapper.Map<List<GetShiftsByEmployeeIdResponse>>(shifts);

            return Result<List<GetShiftsByEmployeeIdResponse>>.Success(response);
        }
    }
}
