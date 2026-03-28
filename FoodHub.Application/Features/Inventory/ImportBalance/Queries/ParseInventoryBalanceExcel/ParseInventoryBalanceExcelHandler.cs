using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Inventory.ImportBalance.Queries.ParseInventoryBalanceExcel;

public class ParseInventoryBalanceExcelHandler
    : IRequestHandler<ParseInventoryBalanceExcelQuery, Result<List<ParsedInventoryBalanceResponse>>>
{
    private readonly IInventoryExcelService _excelService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public ParseInventoryBalanceExcelHandler(
        IInventoryExcelService excelService,
        IUnitOfWork unitOfWork,
        IMessageService messageService
    )
    {
        _excelService = excelService;
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public async Task<Result<List<ParsedInventoryBalanceResponse>>> Handle(
        ParseInventoryBalanceExcelQuery request,
        CancellationToken cancellationToken
    )
    {
        if (request.File == null || request.File.Length == 0)
        {
            return Result<List<ParsedInventoryBalanceResponse>>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InvalidFile)
            );
        }

        List<InventoryBalanceImportDto> rawItems;
        try
        {
            using var stream = request.File.OpenReadStream();
            rawItems = await _excelService.ParseExcelFileAsync(stream, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<List<ParsedInventoryBalanceResponse>>.Failure(
                _messageService.GetMessage(MessageKeys.Common.ExcelRequired)
            );
        }

        if (rawItems.Count == 0)
        {
            return Result<List<ParsedInventoryBalanceResponse>>.Success(
                new List<ParsedInventoryBalanceResponse>()
            );
        }

        var codes = rawItems.Select(x => x.IngredientCode).Distinct().ToList();
        var ingredients = await _unitOfWork
            .Repository<Ingredient>()
            .Query()
            .Where(x => codes.Contains(x.Code) && x.IsActive)
            .ToListAsync(cancellationToken);

        var ingredientMap = ingredients.ToDictionary(x => x.Code);

        var result = rawItems
            .Select(item =>
            {
                var exists = ingredientMap.TryGetValue(item.IngredientCode, out var ingredient);
                return new ParsedInventoryBalanceResponse
                {
                    IngredientId = ingredient?.IngredientId.ToString() ?? string.Empty,
                    IngredientCode = item.IngredientCode,
                    IngredientName = ingredient?.Name,
                    Quantity = item.Quantity,
                    CostPrice = item.CostPrice,
                    Unit = item.Unit ?? ingredient?.BaseUnit.ToString(),
                    RowNumber = item.RowNumber,
                    IsExist = exists,
                };
            })
            .ToList();

        return Result<List<ParsedInventoryBalanceResponse>>.Success(result);
    }
}
