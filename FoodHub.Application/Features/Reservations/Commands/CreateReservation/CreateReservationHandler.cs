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
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Tìm các bàn khả dụng trong khu vực, đủ sức chứa, không lỗi
                var tables = await _unitOfWork.Repository<Table>()
                    .Query()
                    .Include(t => t.Area)
                    .Where(t => t.AreaId == request.AreaId && t.Status != TableStatus.OutOfService && t.Capacity >= request.GuestCount)
                    .OrderBy(t => t.Capacity) // Ưu tiên bàn nhỏ nhất đủ chỗ
                    .ToListAsync(cancellationToken);

                if (!tables.Any())
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<CreateReservationResponse>.Failure(_messageService.GetMessage(MessageKeys.Table.NotFound), ResultErrorType.NotFound);
                }

                // Check overlapping: Nếu bàn đã có người đặt (Booked) hoặc đang ngồi (CheckIn) trong ngày hôm đó thì không cho đặt nữa
                var tableIds = tables.Select(t => t.TableId).ToList();

                var overlappedTableIds = await _unitOfWork.Repository<Reservation>().Query()
                    .Where(r => tableIds.Contains(r.TableId)
                                && r.ReservationDate == request.ReservationDate 
                                && (r.Status == ReservationStatus.Booked || r.Status == ReservationStatus.CheckIn))
                    .Select(r => r.TableId)
                    .ToListAsync(cancellationToken);

                var selectedTable = tables.FirstOrDefault(t => !overlappedTableIds.Contains(t.TableId));

                if (selectedTable == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<CreateReservationResponse>.Failure(_messageService.GetMessage(MessageKeys.Reservation.Overlapped), ResultErrorType.Conflict);
                }


                var reservation = new Reservation
                {
                    ReservationId = Guid.NewGuid(),
                    CustomerName = request.CustomerName,
                    CustomerPhone = request.CustomerPhone,
                    ReservationDate = request.ReservationDate,
                    ReservationTime = request.ReservationTime,
                    PartyType = request.PartyType,
                    GuestCount = request.GuestCount,
                    Note = request.Note,
                    Status = ReservationStatus.Booked,
                    TableId = selectedTable.TableId,
                    AreaId = selectedTable.AreaId
                };
                await _unitOfWork.Repository<Reservation>().AddAsync(reservation);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Successfully created Reservation ID {ReservationId} for Table {TableId}", reservation.ReservationId, selectedTable.TableId);

                await _cacheService.RemoveByPatternAsync(CacheKey.ReservationList + "*");

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
                return Result<CreateReservationResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalServerError));
            }
        }
    }
}
