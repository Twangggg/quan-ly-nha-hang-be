using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.KDS.Queries.GetKdsQueue
{
    public class GetKdsQueueHandler
        : IRequestHandler<GetKdsQueueQuery, Result<List<KdsQueueResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetKdsQueueHandler> _logger;

        public GetKdsQueueHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetKdsQueueHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<List<KdsQueueResponse>>> Handle(
            GetKdsQueueQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Fetching KDS Queue for Station: {Station}", request.Station);

            var orderItemRepository = _unitOfWork.Repository<OrderItem>();

            var queueItems = await orderItemRepository
                .Query()
                .AsNoTracking()
                .Where(oi =>
                    oi.StationSnapshot == request.Station && oi.Status == OrderItemStatus.Preparing
                )
                .OrderBy(oi => oi.CreatedAt)
                .ProjectTo<KdsQueueResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            for (int i = 0; i < queueItems.Count; i++)
            {
                queueItems[i].QueuePosition = i + 1;
            }

            _logger.LogInformation(
                "Successfully fetched {Count} items in Queue for Station: {Station}",
                queueItems.Count,
                request.Station
            );

            return Result<List<KdsQueueResponse>>.Success(queueItems);
        }
    }
}
