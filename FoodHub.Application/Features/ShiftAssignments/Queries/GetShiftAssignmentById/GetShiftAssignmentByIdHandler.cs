using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.ShiftAssignments.Queries.GetShiftAssignmentById
{
    public class GetShiftAssignmentByIdHandler
        : IRequestHandler<GetShiftAssignmentByIdQuery, Result<GetShiftAssignmentByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;

        public GetShiftAssignmentByIdHandler(
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

        public async Task<Result<GetShiftAssignmentByIdResponse>> Handle(
            GetShiftAssignmentByIdQuery request,
            CancellationToken cancellationToken)
        {
            var cacheKey = string.Format(CacheKey.ShiftAssignmentById, request.ShiftAssignmentId);

            var cached = await _cacheService.GetAsync<GetShiftAssignmentByIdResponse>(cacheKey, cancellationToken);
            if (cached is not null)
                return Result<GetShiftAssignmentByIdResponse>.Success(cached);

            var assignment = await _unitOfWork.Repository<ShiftAssignment>()
                .Query()
                .Include(a => a.Employee)
                .Include(a => a.Shift)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ShiftAssignmentId == request.ShiftAssignmentId, cancellationToken);

            if (assignment is null)
                return Result<GetShiftAssignmentByIdResponse>.NotFound(_messageService.GetMessage(MessageKeys.ShiftAssignment.NotFound));

            var response = _mapper.Map<GetShiftAssignmentByIdResponse>(assignment);

            await _cacheService.SetAsync(cacheKey, response, CacheTTL.ShiftAssignments, cancellationToken);
            return Result<GetShiftAssignmentByIdResponse>.Success(response);
        }
    }
}
