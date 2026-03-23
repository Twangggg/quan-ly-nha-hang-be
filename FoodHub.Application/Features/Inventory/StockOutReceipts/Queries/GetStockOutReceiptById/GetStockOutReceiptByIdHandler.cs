using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Helpers;
using FoodHub.Application.Constants;
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

namespace FoodHub.Application.Features.Inventory.StockOutReceipts.Queries.GetStockOutReceiptById
{
    public class GetStockOutReceiptByIdHandler
        : IRequestHandler<GetStockOutReceiptByIdQuery, Result<GetStockOutReceiptByIdResponse>>
    {
        private readonly ILogger<GetStockOutReceiptByIdHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public GetStockOutReceiptByIdHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IMessageService messageService,
            ILogger<GetStockOutReceiptByIdHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<GetStockOutReceiptByIdResponse>> Handle(
            GetStockOutReceiptByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetStockOutReceiptById for StockOutReceiptId={StockOutReceiptId}",
                request.StockOutReceiptId
            );

            var cacheKey = string.Format(CacheKey.InventoryStockOutReceiptById, request.StockOutReceiptId);
            var cached = await _cacheService.GetAsync<GetStockOutReceiptByIdResponse>(
                cacheKey,
                cancellationToken
            );
            if (cached is not null)
            {
                _logger.LogInformation(
                    "End handling GetStockOutReceiptById for StockOutReceiptId={StockOutReceiptId} (from cache)",
                    request.StockOutReceiptId
                );
                return Result<GetStockOutReceiptByIdResponse>.Success(cached);
            }

            var employeeQuery = _unitOfWork.Repository<Employee>().Query().AsNoTracking();
            var ingredientQuery = _unitOfWork.Repository<Ingredient>().Query().AsNoTracking();

            var response = await _unitOfWork
                .Repository<StockOutReceipt>()
                .Query()
                .AsNoTracking()
                .Where(x => x.StockOutReceiptId == request.StockOutReceiptId)
                .Select(x => new GetStockOutReceiptByIdResponse
                {
                    StockOutReceiptId = x.StockOutReceiptId,
                    ReceiptCode = x.ReceiptCode,
                    StockOutDate = x.StockOutDate,
                    Reason = x.Reason,
                    TotalAmount = x.TotalAmount,
                    CreatedByName = employeeQuery
                        .Where(e => e.EmployeeId == x.CreatedBy)
                        .Select(e => e.FullName)
                        .FirstOrDefault(),
                    Items = x
                        .Items.OrderBy(i => i.CreatedAt)
                        .Select(i => new GetStockOutReceiptByIdItemResponse
                        {
                            StockOutReceiptItemId = i.StockOutReceiptItemId,
                            IngredientId = i.IngredientId,
                            IngredientCode =
                                ingredientQuery
                                    .Where(ing => ing.IngredientId == i.IngredientId)
                                    .Select(ing => ing.Code)
                                    .FirstOrDefault()
                                ?? string.Empty,
                            IngredientName =
                                ingredientQuery
                                    .Where(ing => ing.IngredientId == i.IngredientId)
                                    .Select(ing => ing.Name)
                                    .FirstOrDefault()
                                ?? string.Empty,
                            Unit =
                                ingredientQuery
                                    .Where(ing => ing.IngredientId == i.IngredientId)
                                    .Select(ing => ing.BaseUnit)
                                    .FirstOrDefault()
                                ?? string.Empty,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice,
                            LineAmount = i.LineAmount,
                        })
                        .ToList(),
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (response is null)
            {
                throw new NotFoundException(
                    _messageService.GetMessage(MessageKeys.StockOutReceipt.ReceiptNotFound)
                );
            }

            await _cacheService.SetAsync(
                cacheKey,
                response,
                CacheTTL.Inventory,
                cancellationToken
            );

            return Result<GetStockOutReceiptByIdResponse>.Success(response);
        }
    }
}
