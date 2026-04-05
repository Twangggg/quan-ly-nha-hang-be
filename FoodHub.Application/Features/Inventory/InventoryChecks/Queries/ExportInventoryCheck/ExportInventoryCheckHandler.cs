using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Queries.ExportInventoryCheck;

public class ExportInventoryCheckHandler
    : IRequestHandler<ExportInventoryCheckQuery, Result<ExportInventoryCheckResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public ExportInventoryCheckHandler(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public async Task<Result<ExportInventoryCheckResponse>> Handle(
        ExportInventoryCheckQuery request,
        CancellationToken cancellationToken
    )
    {
        var inventoryCheck = await _unitOfWork
            .Repository<InventoryCheck>()
            .Query()
            .Include(x => x.Items)
            .ThenInclude(x => x.Ingredient)
            .FirstOrDefaultAsync(x => x.InventoryCheckId == request.InventoryCheckId, cancellationToken);

        if (inventoryCheck == null)
        {
            throw new NotFoundException(
                _messageService.GetMessage(MessageKeys.InventoryCheck.CheckNotFound)
            );
        }

        var response = new ExportInventoryCheckResponse
        {
            InventoryCheckId = inventoryCheck.InventoryCheckId,
            CheckDate = inventoryCheck.CheckDate,
            Status = inventoryCheck.Status.ToString(),
            CreatedAt = inventoryCheck.CreatedAt,
            TotalItems = inventoryCheck.Items.Count
        };

        var items = new List<ExportInventoryCheckItemResponse>();
        decimal totalBookValue = 0;
        decimal totalPhysicalValue = 0;
        decimal totalDifferenceValue = 0;

        foreach (var item in inventoryCheck.Items)
        {
            var bookValue = item.BookQuantity * item.Ingredient.CostPrice;
            var physicalValue = item.PhysicalQuantity * item.Ingredient.CostPrice;
            var differenceValue = item.DifferenceQuantity * item.Ingredient.CostPrice;

            items.Add(new ExportInventoryCheckItemResponse
            {
                IngredientCode = item.Ingredient.Code,
                IngredientName = item.Ingredient.Name,
                Unit = item.Ingredient.BaseUnit,
                BookQuantity = item.BookQuantity,
                PhysicalQuantity = item.PhysicalQuantity,
                DifferenceQuantity = item.DifferenceQuantity,
                BookValue = bookValue,
                PhysicalValue = physicalValue,
                DifferenceValue = differenceValue,
                Reason = item.Reason
            });

            totalBookValue += bookValue;
            totalPhysicalValue += physicalValue;
            totalDifferenceValue += differenceValue;
        }

        response.Items = items;
        response.TotalBookValue = totalBookValue;
        response.TotalPhysicalValue = totalPhysicalValue;
        response.TotalDifferenceValue = totalDifferenceValue;

        return Result<ExportInventoryCheckResponse>.Success(response);
    }
}
