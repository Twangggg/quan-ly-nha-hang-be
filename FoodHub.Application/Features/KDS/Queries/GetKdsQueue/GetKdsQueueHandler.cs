using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.KDS.Common;
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
        private readonly KdsPriorityCalculator _priorityCalculator;
        private readonly ILogger<GetKdsQueueHandler> _logger;

        public GetKdsQueueHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            KdsPriorityCalculator priorityCalculator,
            ILogger<GetKdsQueueHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _priorityCalculator = priorityCalculator;
            _logger = logger;
        }

        public async Task<Result<List<KdsQueueResponse>>> Handle(
            GetKdsQueueQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Fetching KDS Queue for Station: {Station}", request.Station);

            var orderItemRepository = _unitOfWork.Repository<OrderItem>();

            var items = await orderItemRepository
                .Query()
                .AsNoTracking()
                .Include(oi => oi.Order)
                .Where(oi =>
                    oi.StationSnapshot == request.Station && oi.Status == OrderItemStatus.Preparing
                )
                .ToListAsync(cancellationToken);

            var responses = _mapper.Map<List<KdsQueueResponse>>(items);

            // Tính điểm ưu tiên
            foreach (var response in responses)
            {
                var originalItem = items.First(i => i.OrderItemId == response.OrderItemId);
                response.PriorityScore = _priorityCalculator.Calculate(
                    originalItem,
                    originalItem.Order
                );
            }

            // Sắp xếp theo điểm trước khi gán vị trí hàng chờ
            var sortedQueue = responses
                .OrderByDescending(oi => oi.PriorityScore)
                .ThenBy(oi => oi.CreatedAt)
                .ToList();

            for (int i = 0; i < sortedQueue.Count; i++)
            {
                sortedQueue[i].QueuePosition = i + 1;
            }

            _logger.LogInformation(
                "Successfully fetched and scored {Count} items in Queue for Station: {Station}",
                sortedQueue.Count,
                request.Station
            );

            return Result<List<KdsQueueResponse>>.Success(sortedQueue);
        }
    }
}
