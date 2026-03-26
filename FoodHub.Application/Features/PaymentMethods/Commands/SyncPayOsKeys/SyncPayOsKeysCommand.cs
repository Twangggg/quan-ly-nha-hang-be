using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.PaymentMethods.Commands.SyncPayOsKeys
{
    public class SyncPayOsKeysCommand : IRequest<Result<bool>>
    {
        // PayOS API Keys
        public string ClientId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ChecksumKey { get; set; } = string.Empty;

        // Bank Account Info (scraped from PayOS page)
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountHolderName { get; set; }
    }
}
