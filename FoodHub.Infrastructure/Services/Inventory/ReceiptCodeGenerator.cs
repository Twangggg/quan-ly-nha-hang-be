using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure.Services.Inventory;

public class ReceiptCodeGenerator : IReceiptCodeGenerator
{
    private readonly IUnitOfWork _unitOfWork;

    public ReceiptCodeGenerator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<string> GenerateStockInReceiptCodeAsync(
        DateTime receivedAt,
        CancellationToken cancellationToken = default
    )
    {
        var datePart = receivedAt.ToString("yyyyMMdd");
        var prefix = $"NK-{datePart}-";

        var lastReceipt = await _unitOfWork
            .Repository<StockInReceipt>()
            .Query()
            .Where(x => x.ReceiptCode.StartsWith(prefix))
            .OrderByDescending(x => x.ReceiptCode)
            .FirstOrDefaultAsync(cancellationToken);

        var sequenceNumber = 1;
        if (lastReceipt is not null)
        {
            var parts = lastReceipt.ReceiptCode.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var lastSequence))
            {
                sequenceNumber = lastSequence + 1;
            }
        }

        return $"{prefix}{sequenceNumber:D4}";
    }

    public async Task<string> GenerateStockOutReceiptCodeAsync(
        DateTime stockOutDate,
        CancellationToken cancellationToken = default
    )
    {
        var datePart = stockOutDate.ToString("yyyyMMdd");
        var prefix = $"XK-{datePart}-";

        var lastReceipt = await _unitOfWork
            .Repository<StockOutReceipt>()
            .Query()
            .Where(x => x.ReceiptCode.StartsWith(prefix))
            .OrderByDescending(x => x.ReceiptCode)
            .FirstOrDefaultAsync(cancellationToken);

        var sequenceNumber = 1;
        if (lastReceipt is not null)
        {
            var parts = lastReceipt.ReceiptCode.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var lastSequence))
            {
                sequenceNumber = lastSequence + 1;
            }
        }

        return $"{prefix}{sequenceNumber:D4}";
    }
}
