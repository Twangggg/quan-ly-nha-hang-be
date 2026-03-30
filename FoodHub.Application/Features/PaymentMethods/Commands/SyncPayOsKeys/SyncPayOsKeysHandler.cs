using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.PaymentMethods.Commands.SyncPayOsKeys
{
    public class SyncPayOsKeysHandler : IRequestHandler<SyncPayOsKeysCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SyncPayOsKeysHandler> _logger;

        public SyncPayOsKeysHandler(IUnitOfWork unitOfWork, ILogger<SyncPayOsKeysHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(SyncPayOsKeysCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Syncing PayOS Keys from Auto-Config Extension.");

            var bankTransferMethod = await _unitOfWork.Repository<PaymentMethodConfig>()
                .Query()
                .FirstOrDefaultAsync(pm => pm.Type == PaymentMethodType.BankTransfer && pm.IsActive, cancellationToken);

            if (bankTransferMethod == null)
            {
                _logger.LogInformation("No active BankTransfer config found. Auto-creating one to host PayOS keys.");
                
                bankTransferMethod = new PaymentMethodConfig
                {
                    PaymentMethodConfigId = Guid.NewGuid(),
                    Name = "Chuyển khoản Ngân hàng (PayOS)",
                    Type = PaymentMethodType.BankTransfer,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    PayOsClientId = request.ClientId,
                    PayOsApiKey = request.ApiKey,
                    PayOsChecksumKey = request.ChecksumKey,
                    BankName = request.BankName,
                    AccountNumber = request.AccountNumber,
                    AccountHolderName = request.AccountHolderName
                };
                
                await _unitOfWork.Repository<PaymentMethodConfig>().AddAsync(bankTransferMethod);
            }
            else
            {
                // Update keys + bank info
                bankTransferMethod.PayOsClientId = request.ClientId;
                bankTransferMethod.PayOsApiKey = request.ApiKey;
                bankTransferMethod.PayOsChecksumKey = request.ChecksumKey;

                // Update bank account if provided
                if (!string.IsNullOrWhiteSpace(request.BankName))
                    bankTransferMethod.BankName = request.BankName;
                if (!string.IsNullOrWhiteSpace(request.AccountNumber))
                    bankTransferMethod.AccountNumber = request.AccountNumber;
                if (!string.IsNullOrWhiteSpace(request.AccountHolderName))
                    bankTransferMethod.AccountHolderName = request.AccountHolderName;

                bankTransferMethod.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Repository<PaymentMethodConfig>().Update(bankTransferMethod);
            }

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation("Successfully synced PayOS keys to BankTransfer config id {Id}", bankTransferMethod.PaymentMethodConfigId);

            return Result<bool>.Success(true);
        }
    }
}
