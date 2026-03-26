using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryCheckById
{
    public class GetInventoryCheckByIdHandler
        : IRequestHandler<GetInventoryCheckByIdQuery, Result<GetInventoryCheckByIdResponse>>
    {
        private readonly ILogger<GetInventoryCheckByIdHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public GetInventoryCheckByIdHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IMessageService messageService,
            ILogger<GetInventoryCheckByIdHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<GetInventoryCheckByIdResponse>> Handle(
            GetInventoryCheckByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetInventoryCheckById for InventoryCheckId={InventoryCheckId}",
                request.InventoryCheckId
            );

            var cacheKey = string.Format(CacheKey.InventoryCheckById, request.InventoryCheckId);
            var cached = await _cacheService.GetAsync<GetInventoryCheckByIdResponse>(
                cacheKey,
                cancellationToken
            );
            if (cached is not null)
            {
                _logger.LogInformation(
                    "End handling GetInventoryCheckById for InventoryCheckId={InventoryCheckId} (from cache)",
                    request.InventoryCheckId
                );
                return Result<GetInventoryCheckByIdResponse>.Success(cached);
            }

            var employeeQuery = _unitOfWork.Repository<Employee>().Query().AsNoTracking();

            var response = await _unitOfWork
                .Repository<InventoryCheck>()
                .Query()
                .AsNoTracking()
                .Where(x => x.InventoryCheckId == request.InventoryCheckId)
                .Select(x => new GetInventoryCheckByIdResponse
                {
                    InventoryCheckId = x.InventoryCheckId,
                    CheckDate = x.CheckDate,
                    Status = x.Status,
                    CreatedByName = employeeQuery
                        .Where(e => e.EmployeeId == x.CreatedBy)
                        .Select(e => e.FullName)
                        .FirstOrDefault(),
                    CreatedAt = x.CreatedAt,
                    TotalItems = x.Items.Count,
                    Items = x
                        .Items.OrderBy(i => i.CreatedAt)
                        .Select(i => new GetInventoryCheckByIdItemResponse
                        {
                            InventoryCheckItemId = i.InventoryCheckItemId,
                            InventoryCheckId = i.InventoryCheckId,
                            IngredientId = i.IngredientId,
                            IngredientCode = i.Ingredient.Code,
                            IngredientName = i.Ingredient.Name,
                            Unit = i.Ingredient.BaseUnit,
                            BookQuantity = i.BookQuantity,
                            PhysicalQuantity = i.PhysicalQuantity,
                            DifferenceQuantity = i.DifferenceQuantity,
                            Reason = i.Reason,
                        })
                        .ToList(),
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (response is null)
            {
                throw new NotFoundException(
                    _messageService.GetMessage(MessageKeys.InventoryCheck.CheckNotFound)
                );
            }

            _logger.LogInformation(
                "End handling GetInventoryCheckById for InventoryCheckId={InventoryCheckId}",
                response.InventoryCheckId
            );

            await _cacheService.SetAsync(
                cacheKey,
                response,
                CacheTTL.Inventory,
                cancellationToken
            );

            return Result<GetInventoryCheckByIdResponse>.Success(response);
        }
    }
}
