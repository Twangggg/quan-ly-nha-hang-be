using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Reservations.Commands.CancelReservation
{
    public class CancelReservationHandler : IRequestHandler<CancelReservationCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CancelReservationHandler> _logger;
        private readonly IMessageService _messageService;

        public CancelReservationHandler(IUnitOfWork unitOfWork, ILogger<CancelReservationHandler> logger, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;

        }

        public async Task<Result<string>> Handle(CancelReservationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cancelling reservation {ReservationId}", request.ReservationId);

            var reservation = await _unitOfWork.Repository<Reservation>().GetByIdAsync(request.ReservationId);
            if (reservation == null)
            {
                return Result<string>.Failure(MessageKeys.Reservation.NotFound);
            }

            reservation.Status = ReservationStatus.Cancelled;

            _unitOfWork.Repository<Reservation>().Update(reservation);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation("Successfully cancelled Reservation {ReservationId}", request.ReservationId);

            return Result<string>.Success(_messageService.GetMessage(MessageKeys.Reservation.CancelReservationSuccess));
        }
    }
}
