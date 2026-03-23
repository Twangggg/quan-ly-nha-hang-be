using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Shifts.Queries.GetShiftById
{
    public class GetShiftByIdHandler : IRequestHandler<GetShiftByIdQuery, Result<GetShiftByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;

        public GetShiftByIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _messageService = messageService;
        }

        public async Task<Result<GetShiftByIdResponse>> Handle(
            GetShiftByIdQuery request,
            CancellationToken cancellationToken)
        {
            var cacheKey = string.Format(CacheKey.ShiftById, request.ShiftId);

            var cached = await _cacheService.GetAsync<GetShiftByIdResponse>(cacheKey, cancellationToken);
            if (cached is not null)
                return Result<GetShiftByIdResponse>.Success(cached);

            var shift = await _unitOfWork.Repository<Shift>()
                .Query()
                .Where(s => s.ShiftId == request.ShiftId)
                .ProjectTo<GetShiftByIdResponse>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            if (shift is null)
            {
                return Result<GetShiftByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Shift.NotFound),
                    ResultErrorType.NotFound);
            }

            await _cacheService.SetAsync(cacheKey, shift, CacheTTL.Shifts, cancellationToken);
            return Result<GetShiftByIdResponse>.Success(shift);
        }
    }
}
