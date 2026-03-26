using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reservations;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FoodHub.Application.Features.Reservations.Settings.Commands.UpdateReservationSettings
{
    public class UpdateReservationSettingsHandler
        : IRequestHandler<UpdateReservationSettingsCommand, Result<UpdateReservationSettingsResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReservationSettingsProvider _reservationSettingsProvider;
        private readonly IMessageService _messageService;
        private readonly ILogger<UpdateReservationSettingsHandler> _logger;

        public UpdateReservationSettingsHandler(
            IUnitOfWork unitOfWork,
            IReservationSettingsProvider reservationSettingsProvider,
            IMessageService messageService,
            ILogger<UpdateReservationSettingsHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _reservationSettingsProvider = reservationSettingsProvider;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<UpdateReservationSettingsResponse>> Handle(
            UpdateReservationSettingsCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling UpdateReservationSettings with OverlapBufferMinutes={OverlapBufferMinutes}, MinLeadTimeMinutes={MinLeadTimeMinutes}",
                request.OverlapBufferMinutes,
                request.MinLeadTimeMinutes
            );

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var settings = await _reservationSettingsProvider.GetOrCreateAsync(cancellationToken);

                var domainResult = settings.Update(
                    ParseTime(request.OpenTime),
                    ParseTime(request.CloseTime),
                    request.BreakEnabled,
                    ParseTime(request.BreakStart),
                    ParseTime(request.BreakEnd),
                    request.OverlapBufferMinutes,
                    request.MinLeadTimeMinutes,
                    request.GracePeriodMinutes
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

                var response = MapToResponse(settings);

                _logger.LogInformation("End handling UpdateReservationSettings");
                return Result<UpdateReservationSettingsResponse>.Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private static UpdateReservationSettingsResponse MapToResponse(ReservationSettings settings)
        {
            return new UpdateReservationSettingsResponse
            {
                OpenTime = FormatTime(settings.OpenTime),
                CloseTime = FormatTime(settings.CloseTime),
                BreakEnabled = settings.BreakEnabled,
                BreakStart = FormatTime(settings.BreakStart),
                BreakEnd = FormatTime(settings.BreakEnd),
                OverlapBufferMinutes = settings.OverlapBufferMinutes,
                MinLeadTimeMinutes = settings.MinLeadTimeMinutes,
                GracePeriodMinutes = settings.GracePeriodMinutes,
            };
        }

        private static TimeOnly ParseTime(string value)
        {
            return TimeOnly.Parse(value, CultureInfo.InvariantCulture);
        }

        private static string FormatTime(TimeOnly time)
        {
            return time.ToString("HH:mm", CultureInfo.InvariantCulture);
        }
    }
}
