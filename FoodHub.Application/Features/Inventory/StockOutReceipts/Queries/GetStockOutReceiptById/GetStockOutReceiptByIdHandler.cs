using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
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

        public GetStockOutReceiptByIdHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ILogger<GetStockOutReceiptByIdHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
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
                                    .Select(ing => ing.Unit)
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

            return Result<GetStockOutReceiptByIdResponse>.Success(response);
        }
    }
}
