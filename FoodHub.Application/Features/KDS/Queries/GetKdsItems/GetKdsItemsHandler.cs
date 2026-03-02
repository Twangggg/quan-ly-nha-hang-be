using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
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
        private readonly ILogger<GetKdsItemsHandler> _logger;

        public GetKdsItemsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetKdsItemsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<List<KdsItemResponse>>> Handle(
            GetKdsItemsQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Fetching KDS items for Station: {Station}", request.Station);

            var orderItemRepository = _unitOfWork.Repository<OrderItem>();

            var items = await orderItemRepository
                .Query()
                .AsNoTracking()
                .Where(oi =>
                    oi.StationSnapshot == request.Station
                    && (
                        oi.Status == OrderItemStatus.Preparing
                        || oi.Status == OrderItemStatus.Cooking
                    )
                )
                .OrderBy(oi => oi.Status == OrderItemStatus.Cooking ? 0 : 1)
                .ThenBy(oi => oi.CreatedAt)
                .ProjectTo<KdsItemResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully fetched {Count} KDS items for Station: {Station}",
                items.Count,
                request.Station
            );

            return Result<List<KdsItemResponse>>.Success(items);
        }
    }
}
