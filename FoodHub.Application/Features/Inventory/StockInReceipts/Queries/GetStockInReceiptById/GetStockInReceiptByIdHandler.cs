using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
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

namespace FoodHub.Application.Features.Inventory.StockInReceipts.Queries.GetStockInReceiptById
{
    public class GetStockInReceiptByIdHandler
        : IRequestHandler<GetStockInReceiptByIdQuery, Result<GetStockInReceiptByIdResponse>>
    {
        private readonly ILogger<GetStockInReceiptByIdHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly IUnitOfWork _unitOfWork;

        public GetStockInReceiptByIdHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ILogger<GetStockInReceiptByIdHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<GetStockInReceiptByIdResponse>> Handle(
            GetStockInReceiptByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetStockInReceiptById for StockInReceiptId={StockInReceiptId}",
                request.StockInReceiptId
            );

            var employeeQuery = _unitOfWork.Repository<Employee>().Query().AsNoTracking();
            var ingredientQuery = _unitOfWork.Repository<Ingredient>().Query().AsNoTracking();

            var response = await _unitOfWork
                .Repository<StockInReceipt>()
                .Query()
                .AsNoTracking()
                .Where(x => x.StockInReceiptId == request.StockInReceiptId)
                .Select(x => new GetStockInReceiptByIdResponse
                {
                    StockInReceiptId = x.StockInReceiptId,
                    ReceiptCode = x.ReceiptCode,
                    ReceivedAt = x.ReceivedAt,
                    Note = x.Note,
                    TotalLines = x.TotalLines,
                    TotalAmount = x.TotalAmount,
                    CreatedByName = employeeQuery
                        .Where(e => e.EmployeeId == x.CreatedBy)
                        .Select(e => e.FullName)
                        .FirstOrDefault(),
                    Items = x
                        .Items.OrderBy(i => i.CreatedAt)
                        .Select(i => new GetStockInReceiptByIdItemResponse
                        {
                            StockInReceiptItemId = i.StockInReceiptItemId,
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
                            BaseUnit = i.BaseUnit,
                            Quantity = i.Quantity,
                            UnitCost = i.UnitCost,
                            LineAmount = i.LineAmount,
                            ExpiryDate = i.ExpiryDate,
                            BatchCode = i.BatchCode,
                        })
                        .ToList(),
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (response is null)
            {
                throw new NotFoundException(
                    _messageService.GetMessage(MessageKeys.StockInReceipt.ReceiptNotFound)
                );
            }

            _logger.LogInformation(
                "End handling GetStockInReceiptById for ReceiptCode={ReceiptCode}",
                response.ReceiptCode
            );

            return Result<GetStockInReceiptByIdResponse>.Success(response);
        }
    }
}
