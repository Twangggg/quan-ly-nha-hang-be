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

namespace FoodHub.Application.Features.KDS.Queries.GetKdsItems
{
    public class GetKdsItemsHandler
        : IRequestHandler<GetKdsItemsQuery, Result<List<KdsItemResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly KdsPriorityCalculator _priorityCalculator;
        private readonly ILogger<GetKdsItemsHandler> _logger;

        public GetKdsItemsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            KdsPriorityCalculator priorityCalculator,
            ILogger<GetKdsItemsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _priorityCalculator = priorityCalculator;
            _logger = logger;
        }

        public async Task<Result<List<KdsItemResponse>>> Handle(
            GetKdsItemsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Fetching KDS items for Station: {Station}", request.Station);

            var orderItemRepository = _unitOfWork.Repository<OrderItem>();

            var targetStations = new List<string> { request.Station };
            if (request.Station.Equals("Kitchen", StringComparison.OrdinalIgnoreCase))
            {
                targetStations.Add(Station.HotKitchen.ToString());
                targetStations.Add(Station.ColdKitchen.ToString());
            }

            var items = await orderItemRepository
                .Query()
                .AsNoTracking()
                .Include(oi => oi.Order) // Bắt buộc phải Include Order để có dữ liệu tính điểm (ví dụ: IsPriority)
                .Include(op => op.OptionGroups)
                    .ThenInclude(og => og.OptionValues)
                .Where(oi =>
                    targetStations.Contains(oi.StationSnapshot)
                    && (
                        oi.Status == OrderItemStatus.Preparing
                        || oi.Status == OrderItemStatus.Cooking
                    )
                )
                .ToListAsync(cancellationToken);

            // Ánh xạ sang DTO
            var responses = _mapper.Map<List<KdsItemResponse>>(items);

            // Tính điểm ưu tiên cho từng món dựa trên logic nghiệp vụ
            foreach (var response in responses)
            {
                var originalItem = items.First(i => i.OrderItemId == response.OrderItemId);
                response.PriorityScore = _priorityCalculator.Calculate(
                    originalItem,
                    originalItem.Order
                );
            }

            // Sắp xếp lại danh sách theo thứ tự tối ưu
            var sortedItems = responses
                .OrderBy(oi => oi.Status == OrderItemStatus.Cooking.ToString() ? 0 : 1) // Đang nấu ưu tiên hiện trước
                .ThenByDescending(oi => oi.PriorityScore) // Điểm ưu tiên cao hơn lên trước
                .ThenBy(oi => oi.CreatedAt) // FIFO fallback: ai đến trước làm trước nếu bằng điểm
                .ToList();

            _logger.LogInformation(
                "Successfully fetched and prioritized {Count} KDS items for Station: {Station}",
                sortedItems.Count,
                request.Station
            );

            return Result<List<KdsItemResponse>>.Success(sortedItems);
        }
    }
}
