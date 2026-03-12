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

                // Xử lý Pre-Order
                if ((request.PreOrderItems != null && request.PreOrderItems.Any()) ||
                    (request.PreOrderSetMenus != null && request.PreOrderSetMenus.Any()))
                {
                    var orderCode = await GenerateOrderCodeAsync(cancellationToken);

                    var preOrder = new Order
                    {
                        OrderId = Guid.NewGuid(),
                        OrderCode = orderCode,
                        OrderType = OrderType.DineIn,
                        Status = OrderStatus.Pending,
                        TableId = selectedTable.TableId,
                        Note = "Pre-order từ đặt bàn Online",
                        TotalAmount = 0,
                        IsPriority = (selectedTable.Area?.Type == AreaType.VIP),
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = null // Khách vãng lai
                    };

                    // Thêm MenuItems rời
                    if (request.PreOrderItems != null && request.PreOrderItems.Any())
                    {
                        var menuItemIds = request.PreOrderItems.Select(x => x.MenuItemId).ToList();
                        var menuItems = await _unitOfWork.Repository<MenuItem>().Query()
                            .Include(m => m.OptionGroups).ThenInclude(og => og.OptionItems)
                            .Where(m => menuItemIds.Contains(m.MenuItemId))
                            .ToDictionaryAsync(m => m.MenuItemId, cancellationToken);

                        var allGroupIds = request.PreOrderItems.SelectMany(x => x.SelectedOptions ?? new()).Select(x => x.OptionGroupId).ToList();
                        var allItemIds = request.PreOrderItems.SelectMany(x => x.SelectedOptions ?? new()).SelectMany(x => x.SelectedValues).Select(x => x.OptionItemId).ToList();
                        
                        var optionGroups = await _unitOfWork.Repository<OptionGroup>().Query().Where(g => allGroupIds.Contains(g.OptionGroupId)).ToDictionaryAsync(g => g.OptionGroupId, cancellationToken);
                        var optionItems = await _unitOfWork.Repository<OptionItem>().Query().Where(i => allItemIds.Contains(i.OptionItemId)).ToDictionaryAsync(i => i.OptionItemId, cancellationToken);

                        foreach (var reqItem in request.PreOrderItems)
                        {
                            if (menuItems.TryGetValue(reqItem.MenuItemId, out var mi) && !mi.IsOutOfStock)
                            {
                                var domainOptions = new List<(OptionGroup Group, List<(OptionItem Item, int Quantity, string? Note)> Selections)>();
                                if (reqItem.SelectedOptions != null)
                                {
                                    foreach (var optDto in reqItem.SelectedOptions)
                                    {
                                        if (optionGroups.TryGetValue(optDto.OptionGroupId, out var og))
                                        {
                                            var selections = optDto.SelectedValues
                                                .Where(v => optionItems.ContainsKey(v.OptionItemId))
                                                .Select(v => (optionItems[v.OptionItemId], v.Quantity, v.Note))
                                                .ToList();
                                            domainOptions.Add((og, selections));
                                        }
                                    }
                                }
                                preOrder.AddOrUpdateItem(mi, reqItem.Quantity, reqItem.Note, domainOptions);
                            }
                        }
                    }

                    // Thêm SetMenus
                    if (request.PreOrderSetMenus != null && request.PreOrderSetMenus.Any())
                    {
                        var setMenuIds = request.PreOrderSetMenus.Select(x => x.SetMenuId).ToList();
                        var setMenus = await _unitOfWork.Repository<SetMenu>().Query()
                            .Include(sm => sm.SetMenuItems).ThenInclude(smi => smi.MenuItem)
                            .Where(sm => setMenuIds.Contains(sm.SetMenuId))
                            .ToDictionaryAsync(sm => sm.SetMenuId, cancellationToken);

                        foreach (var reqSetMenu in request.PreOrderSetMenus)
                        {
                            if (setMenus.TryGetValue(reqSetMenu.SetMenuId, out var sm) && !sm.IsOutOfStock)
                            {
                                foreach (var smi in sm.SetMenuItems)
                                {
                                    if (!smi.MenuItem.IsOutOfStock) 
                                    {
                                        var emptyOptions = new List<(OptionGroup Group, List<(OptionItem Item, int Quantity, string? Note)> Selections)>();
                                        var itemNote = $"[Combo: {sm.Name}] " + (reqSetMenu.Note ?? "");
                                        preOrder.AddOrUpdateItem(smi.MenuItem, smi.Quantity * reqSetMenu.Quantity, itemNote, emptyOptions);
                                    }
                                }
                            }
                        }
                    }

                    if (preOrder.OrderItems.Any())
                    {
                        await _unitOfWork.Repository<Order>().AddAsync(preOrder);
                        reservation.OrderId = preOrder.OrderId;
                    }
                }

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
                _logger.LogError(ex, "Lỗi khi tạo đặt bàn và order kèm theo.");
                return Result<CreateReservationResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalServerError), ResultErrorType.Conflict);
            }
        }

        private async Task<string> GenerateOrderCodeAsync(CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;
            var dateString = today.ToString("yyyyMMdd");
            var prefix = $"ORD-{dateString}-";

            var lastOrder = await _unitOfWork
                .Repository<Order>()
                .Query()
                .Where(o => o.OrderCode.StartsWith(prefix))
                .OrderByDescending(o => o.OrderCode)
                .FirstOrDefaultAsync(cancellationToken);

            int sequenceNumber = 1;
            if (lastOrder != null)
            {
                var parts = lastOrder.OrderCode.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSequence))
                {
                    sequenceNumber = lastSequence + 1;
                }
            }

            return $"{prefix}{sequenceNumber:D4}";
        }
    }
}
