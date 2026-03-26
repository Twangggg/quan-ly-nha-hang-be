using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.PaymentMethods.Commands.TogglePaymentMethodStatus
{
    public class TogglePaymentMethodStatusHandler
        : IRequestHandler<TogglePaymentMethodStatusCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TogglePaymentMethodStatusHandler> _logger;
        private readonly IMessageService _messageService;

        public TogglePaymentMethodStatusHandler(
            IUnitOfWork unitOfWork,
            ILogger<TogglePaymentMethodStatusHandler> logger,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<bool>> Handle(
            TogglePaymentMethodStatusCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Toggling PaymentMethodConfig status: {Id}", request.PaymentMethodConfigId);

            var entity = await _unitOfWork.Repository<PaymentMethodConfig>()
                .Query()
                .FirstOrDefaultAsync(p => p.PaymentMethodConfigId == request.PaymentMethodConfigId, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("PaymentMethodConfig not found: {Id}", request.PaymentMethodConfigId);
                return Result<bool>.Failure(
                    _messageService.GetMessage(MessageKeys.PaymentMethodConfig.NotFound),
                    ResultErrorType.NotFound);
            }

            if (entity.IsActive)
            {
                var domainResult = entity.Deactivate();
                if (!domainResult.IsSuccess)
                {
                    _logger.LogWarning("Cannot deactivate PaymentMethodConfig: {Id}. Reason: {Error}",
                        request.PaymentMethodConfigId, domainResult.ErrorCode);
                    return Result<bool>.Failure(
                        _messageService.GetMessage(MessageKeys.PaymentMethodConfig.CannotDeactivateDefault),
                        ResultErrorType.BadRequest);
                }
            }
            else
            {
                entity.Activate();
            }

            _unitOfWork.Repository<PaymentMethodConfig>().Update(entity);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation("PaymentMethodConfig {Id} toggled to IsActive={IsActive}",
                entity.PaymentMethodConfigId, entity.IsActive);

            return Result<bool>.Success(entity.IsActive);
        }
    }
}
