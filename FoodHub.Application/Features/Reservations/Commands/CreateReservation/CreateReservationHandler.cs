using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Reservations.Commands.CreateReservation
{
    public class CreateReservationHandler : IRequestHandler<CreateReservationCommand, Result<CreateReservationResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateReservationHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;

        public CreateReservationHandler(
            IUnitOfWork unitOfWork,
            ILogger<CreateReservationHandler> logger,
            IMessageService messageService,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
            _cacheService = cacheService;
        }

        public async Task<Result<CreateReservationResponse>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Creating reservation request for Area {AreaId} on {ReservationDate} at {ReservationTime} for {GuestCount} guests",
                request.AreaId,
                request.ReservationDate,
                request.ReservationTime,
                request.GuestCount
            );

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var tables = await _unitOfWork.Repository<Table>()
                    .Query()
                    .Where(t => t.AreaId == request.AreaId
                        && t.Status != TableStatus.OutOfService
                        && t.Capacity >= request.GuestCount)
                    .OrderBy(t => t.Capacity)
                    .ToListAsync(cancellationToken);

                if (!tables.Any())
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<CreateReservationResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Table.NotFound),
                        ResultErrorType.NotFound
                    );
                }

                var tableIds = tables.Select(t => t.TableId).ToList();
                var candidateReservations = tables.ToDictionary(
                    table => table.TableId,
                    table => Reservation.CreateBooked(
                        request.CustomerName,
                        request.CustomerPhone,
                        request.ReservationDate,
                        request.ReservationTime,
                        request.GuestCount,
                        request.Note,
                        table.TableId,
                        table.AreaId
                    )
                );

                var existingReservations = await _unitOfWork.Repository<Reservation>()
                    .Query()
                    .Where(r => tableIds.Contains(r.TableId)
                        && r.ReservationDate == request.ReservationDate
                        && (r.Status == ReservationStatus.Booked || r.Status == ReservationStatus.CheckIn))
                    .ToListAsync(cancellationToken);

                var selectedTable = tables.FirstOrDefault(table =>
                    existingReservations.All(existingReservation =>
                        existingReservation.TableId != table.TableId
                        || !candidateReservations[table.TableId].OverlapsWith(existingReservation)
                    )
                );

                if (selectedTable == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<CreateReservationResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Reservation.Overlapped),
                        ResultErrorType.Conflict
                    );
                }

                var reservation = candidateReservations[selectedTable.TableId];

                await _unitOfWork.Repository<Reservation>().AddAsync(reservation);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Successfully created Reservation ID {ReservationId} for Table {TableId}",
                    reservation.ReservationId,
                    selectedTable.TableId
                );

                await _cacheService.RemoveByPatternAsync(CacheKey.ReservationList + "*", cancellationToken);

                return Result<CreateReservationResponse>.Success(new CreateReservationResponse
                {
                    ReservationId = reservation.ReservationId,
                    TableId = selectedTable.TableId
                });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error occurred while creating reservation.");
                return Result<CreateReservationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InternalServerError)
                );
            }
        }
    }
}
