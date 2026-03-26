using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.KDS.Settings.Queries.GetKdsSettings;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Kds;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.KDS.Settings.Commands.UpdateKdsSettings
{
    public class UpdateKdsSettingsHandler
        : IRequestHandler<UpdateKdsSettingsCommand, Result<UpdateKdsSettingsResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IKdsSettingsProvider _kdsSettingsProvider;
        private readonly IMessageService _messageService;
        private readonly ILogger<UpdateKdsSettingsHandler> _logger;

        public UpdateKdsSettingsHandler(
            IUnitOfWork unitOfWork,
            IKdsSettingsProvider kdsSettingsProvider,
            IMessageService messageService,
            ILogger<UpdateKdsSettingsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _kdsSettingsProvider = kdsSettingsProvider;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<UpdateKdsSettingsResponse>> Handle(
            UpdateKdsSettingsCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling UpdateKdsSettings with SortMode={SortMode}",
                request.SortMode
            );

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var settings = await _kdsSettingsProvider.GetOrCreateAsync(cancellationToken);

                var domainResult = settings.Update(
                    request.SortMode,
                    request.PriorityWeights.WaitTimePerMinute,
                    request.PriorityWeights.OrderPriorityBonus,
                    request.PriorityWeights.ExpectedTimeWeight,
                    request.PriorityWeights.OverduePerMinute,
                    request.PriorityWeights.CompletionBoostWeight,
                    request.PriorityWeights.TakeawayBonus,
                    request.PriorityWeights.DeliveryBonus,
                    request.StationWipLimits.Select(x =>
                        new KdsStationWipLimitConfig(x.Station, x.Limit, x.Enabled)
                    )
                );

                if (!domainResult.IsSuccess)
                {
                    throw new BusinessException(
                        _messageService.GetMessage(
                            domainResult.ErrorCode ?? MessageKeys.Common.ValidationFailed
                        )
                    );
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                var response = GetKdsSettingsHandler.MapToResponse(settings);
                return Result<UpdateKdsSettingsResponse>.Success(
                    new UpdateKdsSettingsResponse
                    {
                        SortMode = response.SortMode,
                        PriorityWeights = response.PriorityWeights,
                        StationWipLimits = response.StationWipLimits,
                    }
                );
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
