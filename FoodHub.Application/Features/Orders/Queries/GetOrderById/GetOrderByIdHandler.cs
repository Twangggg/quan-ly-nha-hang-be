using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Orders.Queries.GetOrderById
{
    public class GetOrderByIdHandler
        : IRequestHandler<GetOrderByIdQuery, Result<GetOrderByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetOrderByIdHandler> _logger;

        public GetOrderByIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<GetOrderByIdHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<GetOrderByIdResponse>> Handle(
            GetOrderByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation("Get order by id: {OrderId}", request.OrderId);

            var orderRepository = _unitOfWork.Repository<Order>();
            var order = await orderRepository
                .Query()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OptionGroups)
                    .ThenInclude(og => og.OptionValues)
                .Include(o => o.Promotion)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result<GetOrderByIdResponse>.NotFound(
                    $"Order with ID {request.OrderId} was not found."
                );
            }

            var response = _mapper.Map<GetOrderByIdResponse>(order);

            _logger.LogInformation("Successfully retrieved order {OrderId}", request.OrderId);

            return Result<GetOrderByIdResponse>.Success(response);
        }
    }
}
