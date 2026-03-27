using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.ImportBalance.Commands.Import;

public class ImportInventoryBalanceHandler
    : IRequestHandler<ImportInventoryBalanceCommand, Result<ImportInventoryBalanceResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryExcelService _excelService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ImportInventoryBalanceHandler> _logger;

    public ImportInventoryBalanceHandler(
        IUnitOfWork unitOfWork,
        IInventoryExcelService excelService,
        ICurrentUserService currentUserService,
        ILogger<ImportInventoryBalanceHandler> logger
    )
    {
        _unitOfWork = unitOfWork;
        _excelService = excelService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<ImportInventoryBalanceResponse>> Handle(
        ImportInventoryBalanceCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Start importing inventory balance from Excel file");

        if (request.File == null || request.File.Length == 0)
        {
            return Result<ImportInventoryBalanceResponse>.Failure(
                "File Excel không hợp lệ"
            );
        }

        var allowedExtensions = new[] { ".xlsx", ".xls" };
        var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return Result<ImportInventoryBalanceResponse>.Failure(
                "Chỉ chấp nhận file Excel (.xlsx, .xls)"
            );
        }

        List<InventoryBalanceImportDto> items;
        try
        {
            using var stream = request.File.OpenReadStream();
            items = await _excelService.ParseExcelFileAsync(stream, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ImportInventoryBalanceResponse>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Excel file");
            return Result<ImportInventoryBalanceResponse>.Failure("Lỗi khi đọc file Excel");
        }

        if (items.Count == 0)
        {
            return Result<ImportInventoryBalanceResponse>.Failure("File Excel không có dữ liệu");
        }

        var ingredientCodes = items.Select(x => x.IngredientCode).Distinct().ToList();
        
        if (!Guid.TryParse(_currentUserService.UserId, out var actorId))
        {
            return Result<ImportInventoryBalanceResponse>.Failure("Không xác định được người dùng");
        }

        var ingredients = await _unitOfWork
            .Repository<Ingredient>()
            .Query()
            .Where(x => ingredientCodes.Contains(x.Code) && x.IsActive)
            .ToListAsync(cancellationToken);

        var response = new ImportInventoryBalanceResponse
        {
            TotalRows = items.Count
        };

        var ingredientMap = ingredients.ToDictionary(x => x.Code);
        var errors = new List<ImportInventoryBalanceError>();

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            foreach (var item in items)
            {
                if (!ingredientMap.TryGetValue(item.IngredientCode, out var ingredient))
                {
                    errors.Add(new ImportInventoryBalanceError
                    {
                        Row = item.RowNumber,
                        Message = $"Mã nguyên liệu '{item.IngredientCode}' không tồn tại"
                    });
                    continue;
                }

                if (item.Quantity < 0)
                {
                    errors.Add(new ImportInventoryBalanceError
                    {
                        Row = item.RowNumber,
                        Message = "Số lượng phải >= 0"
                    });
                    continue;
                }

                if (!request.ConfirmOverwrite && ingredient.CurrentStock > 0)
                {
                    errors.Add(new ImportInventoryBalanceError
                    {
                        Row = item.RowNumber,
                        Message = $"Nguyên liệu '{ingredient.Name}' đã có tồn kho, cần xác nhận ghi đè"
                    });
                    continue;
                }

                var domainResult = ingredient.SetOpeningStock(
                    item.Quantity,
                    item.CostPrice,
                    actorId
                );

                if (!domainResult.IsSuccess)
                {
                    errors.Add(new ImportInventoryBalanceError
                    {
                        Row = item.RowNumber,
                        Message = domainResult.ErrorCode ?? "Lỗi khi cập nhật tồn kho"
                    });
                    continue;
                }

                response.SuccessCount++;

                // Create transaction
                var transaction = InventoryTransaction.CreateOpeningStock(
                    ingredient.IngredientId,
                    item.Quantity,
                    item.CostPrice,
                    item.Quantity, // balance after is simply the quantity for opening stock
                    $"Nhập tồn kho file Excel: {request.File.FileName}",
                    actorId
                );
                await _unitOfWork.Repository<InventoryTransaction>().AddAsync(transaction);
            }


            response.FailedCount = errors.Count;
            response.Errors = errors;
            response.ImportedAt = DateTime.UtcNow;

            if (response.SuccessCount > 0)
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync();
            }

            _logger.LogInformation(
                "Import completed: Success={SuccessCount}, Failed={FailedCount}",
                response.SuccessCount,
                response.FailedCount
            );

            return Result<ImportInventoryBalanceResponse>.Success(response);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Import inventory balance failed");
            return Result<ImportInventoryBalanceResponse>.Failure("Lỗi khi nhập tồn kho: " + ex.Message);
        }
    }
}