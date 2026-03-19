using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Shifts.Queries.GetShiftById;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Shifts.Queries.GetShifts
{
    public class GetShiftsHandler : IRequestHandler<GetShiftsQuery, Result<List<GetShiftByIdResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public GetShiftsHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<Result<List<GetShiftByIdResponse>>> Handle(
            GetShiftsQuery request,
            CancellationToken cancellationToken)
        {
            var cached = await _cacheService.GetAsync<List<GetShiftByIdResponse>>(
                CacheKey.ShiftList, cancellationToken);

            if (cached is not null)
                return Result<List<GetShiftByIdResponse>>.Success(cached);

            var shifts = await _unitOfWork.Repository<Shift>()
                .Query()
                .OrderBy(s => s.StartTime)
                .ProjectTo<GetShiftByIdResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            await _cacheService.SetAsync(CacheKey.ShiftList, shifts, CacheTTL.Shifts, cancellationToken);
            return Result<List<GetShiftByIdResponse>>.Success(shifts);
        }
    }
}
