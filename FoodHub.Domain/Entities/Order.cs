using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Order : BaseEntity
    {
        public Guid OrderId { get; set; }
        public int TransactionCode { get; set; } // Auto-increment for PayOS mapping
        public string OrderCode { get; set; } = null!; // ORD-YYYYMMDD-XXXX
        public OrderType OrderType { get; set; }
        public OrderStatus Status { get; set; }

        // Nullable because it is required only for DINE_IN
        public Guid? TableId { get; set; }
        public Guid? ReservationId { get; set; }

        public string? Note { get; set; }
        public decimal SubTotal { get; set; }
        public decimal VatRate { get; set; } = 0.10m; // Default 10% VAT
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsPriority { get; set; }

        public virtual Employee? CreatedByEmployee { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        // Navigation properties
        public virtual Table? Table { get; set; }
        public virtual Reservation? Reservation { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public decimal? AmountPaid { get; set; }
        public DateTime? PaidAt { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<OrderAuditLog> OrderAuditLogs { get; set; } = new List<OrderAuditLog>();
        public ICollection<OrderPayment> OrderPayments { get; set; } = new List<OrderPayment>();

        // Promotion
        public decimal DiscountAmount { get; set; }
        public Guid? PromotionId { get; set; }
        public virtual Promotion? Promotion { get; set; }

        public bool IsActive() => Status == OrderStatus.Serving;

        /// <summary>
        /// Ghi nhận thanh toán một phần (tiền mặt). Cộng dồn vào AmountPaid nhưng KHÔNG đổi trạng thái.
        /// Order vẫn ở trạng thái Serving để chờ thanh toán phần còn lại.
        /// </summary>
        public DomainResult AddCashPayment(decimal cashAmount)
        {
            if (Status != OrderStatus.Serving)
            {
                return DomainResult.Failure(DomainErrors.Order.InvalidActionWithStatus);
            }

            if (cashAmount <= 0)
            {
                return DomainResult.Failure(DomainErrors.Order.InsufficientAmount);
            }

            AmountPaid = (AmountPaid ?? 0) + cashAmount;
            UpdatedAt = DateTime.UtcNow;

            return DomainResult.Success();
        }

        /// <summary>
        /// Hoàn tất thanh toán. Gọi khi tổng AmountPaid >= TotalAmount.
        /// Chuyển trạng thái sang Paid.
        /// </summary>
        public DomainResult CompletePayment()
        {
            if (Status == OrderStatus.Paid || Status == OrderStatus.Completed || Status == OrderStatus.Cancelled)
            {
                return DomainResult.Failure(DomainErrors.Order.InvalidActionWithStatus);
            }

            if (Status != OrderStatus.Serving)
            {
                return DomainResult.Failure(DomainErrors.Order.InvalidActionWithStatus);
            }

            if ((AmountPaid ?? 0) < TotalAmount)
            {
                return DomainResult.Failure(DomainErrors.Order.InsufficientAmount);
            }

            Status = OrderStatus.Paid;
            PaidAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            return DomainResult.Success();
        }

        /// <summary>
        /// Legacy: Thanh toán toàn bộ một lần (dùng cho webhook hoặc full checkout).
        /// </summary>
        public DomainResult ProcessCheckout(decimal totalAmountReceived)
        {
            if (Status != OrderStatus.Serving)
            {
                return DomainResult.Failure(DomainErrors.Order.InvalidActionWithStatus);
            }

            AmountPaid = (AmountPaid ?? 0) + totalAmountReceived;

            if ((AmountPaid ?? 0) < TotalAmount)
            {
                return DomainResult.Failure(DomainErrors.Order.InsufficientAmount);
            }

            Status = OrderStatus.Paid;
            PaidAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            return DomainResult.Success();
        }

        public void ReleaseTable()
        {
            TableId = null;
        }

        public bool CanCancel() => Status == OrderStatus.Serving;

        public DomainResult Cancel()
        {
            if (!CanCancel())
            {
                return DomainResult.Failure(DomainErrors.Order.InvalidStatusForCancel);
            }

            var hasNonCancellableItems = OrderItems.Any(item =>
                item.Status == OrderItemStatus.Completed
            );
            if (hasNonCancellableItems)
            {
                return DomainResult.Failure(DomainErrors.Order.InvalidStatusForCancel);
            }

            foreach (var item in OrderItems.Where(item => !item.IsFinished()))
            {
                var itemResult = item.Cancel();
                if (!itemResult.IsSuccess)
                {
                    return DomainResult.Failure(
                        itemResult.ErrorCode ?? DomainErrors.Order.InvalidStatusForCancel
                    );
                }
            }

            if (PromotionId.HasValue)
            {
                if (Promotion != null && Promotion.UsedCount > 0)
                    Promotion.UsedCount--;
            }

            Status = OrderStatus.Cancelled;
            CancelledAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            return DomainResult.Success();
        }

        /// <summary>
        /// Helper: thanh toán toàn bộ một lần (backward-compatible).
        /// </summary>
        public DomainResult Checkout(decimal totalAmountReceived)
            => ProcessCheckout(totalAmountReceived);

        /// <summary>
        /// Lấy số tiền còn lại cần thanh toán.
        /// </summary>
        public decimal GetRemainingAmount()
            => Math.Max(TotalAmount - (AmountPaid ?? 0), 0);

        public bool CanComplete() =>
            Status == OrderStatus.Serving && OrderItems.All(oi => oi.IsFinished());

        public DomainResult Complete()
        {
            if (Status != OrderStatus.Serving)
            {
                return DomainResult.Failure(DomainErrors.Order.OrderNotReadyForCompletion);
            }

            if (OrderItems.Any(oi => !oi.IsFinished()))
            {
                return DomainResult.Failure(DomainErrors.Order.ItemsNotFinished);
            }

            Status = OrderStatus.Completed;
            RecalculateTotalAmount();

            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            return DomainResult.Success();
        }

        public void RecalculateTotalAmount()
        {
            SubTotal = OrderItems.Sum(item => item.GetTotalPrice());

            DiscountAmount = CalculateDiscount();
            var tempAmount = Math.Max(SubTotal - DiscountAmount, 0);
            VatAmount = tempAmount * VatRate;
            TotalAmount = tempAmount + VatAmount;
        }

        public decimal GetPromotionValidationSubTotal()
        {
            return OrderItems.Sum(item => item.GetGrossTotalPrice());
        }

        public void ChangeTable(Guid newTableId, DateTime updatedAt, Guid? updatedBy)
        {
            TableId = newTableId;
            UpdatedAt = updatedAt;
            UpdatedBy = updatedBy;
        }

        public void MarkAsClosed(DateTime updatedAt, Guid? updatedBy)
        {
            Status = OrderStatus.Closed;
            UpdatedAt = updatedAt;
            UpdatedBy = updatedBy;
        }

        public void MarkAsMerged(string destinationOrderCode, DateTime updatedAt, Guid? updatedBy)
        {
            Status = OrderStatus.Merged;
            UpdatedAt = updatedAt;
            UpdatedBy = updatedBy;
            Note = string.IsNullOrWhiteSpace(Note)
                ? $"Merged into Order {destinationOrderCode}"
                : $"{Note}; Merged into Order {destinationOrderCode}";
        }

        public void AppendNote(string? note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                return;
            }

            Note = string.IsNullOrWhiteSpace(Note) ? note : $"{Note}; {note}";
        }

        public static Order CreateSplitOrder(
            string orderCode,
            Order sourceOrder,
            Guid destinationTableId,
            Guid? destinationReservationId,
            DateTime createdAt,
            Guid? createdBy
        )
        {
            return new Order
            {
                OrderId = Guid.NewGuid(),
                OrderCode = orderCode,
                OrderType = sourceOrder.OrderType,
                Status = OrderStatus.Serving,
                TableId = destinationTableId,
                ReservationId = destinationReservationId ?? sourceOrder.ReservationId,
                Note = $"Split from Order {sourceOrder.OrderCode}",
                TotalAmount = 0,
                IsPriority = sourceOrder.IsPriority,
                CreatedAt = createdAt,
                CreatedBy = createdBy,
            };
        }

        public DomainResult<MergeOrderPlan> MergeFrom(
            Order sourceOrder,
            DateTime updatedAt,
            Guid? updatedBy
        )
        {
            if (
                sourceOrder.OrderId == OrderId
                || !IsActive()
                || !sourceOrder.IsActive()
                || OrderType != OrderType.DineIn
                || sourceOrder.OrderType != OrderType.DineIn
            )
            {
                return DomainResult<MergeOrderPlan>.Failure(
                    DomainErrors.Order.InvalidActionWithStatus
                );
            }

            var deletedSourceItems = new List<OrderItem>();

            foreach (var sourceItem in sourceOrder.OrderItems.ToList())
            {
                var existingItem = OrderItems.FirstOrDefault(item =>
                    item.HasSameConfiguration(sourceItem)
                );

                if (existingItem != null)
                {
                    existingItem.IncreaseQuantity(sourceItem.Quantity, updatedAt);
                    sourceOrder.OrderItems.Remove(sourceItem);
                    deletedSourceItems.Add(sourceItem);
                    continue;
                }

                sourceItem.ReassignToOrder(OrderId, updatedAt);
                sourceOrder.OrderItems.Remove(sourceItem);
                OrderItems.Add(sourceItem);
            }

            AppendNote(sourceOrder.Note);
            RecalculateTotalAmount();
            UpdatedAt = updatedAt;
            UpdatedBy = updatedBy;

            sourceOrder.RecalculateTotalAmount();
            sourceOrder.MarkAsMerged(OrderCode, updatedAt, updatedBy);

            return DomainResult<MergeOrderPlan>.Success(new MergeOrderPlan(deletedSourceItems));
        }

        public DomainResult<SplitOrderPlan> SplitItemsTo(
            Order destinationOrder,
            IReadOnlyCollection<OrderItemSplitRequest> splitRequests,
            DateTime updatedAt,
            Guid? updatedBy
        )
        {
            if (
                destinationOrder.OrderId == OrderId
                || !IsActive()
                || !destinationOrder.IsActive()
                || OrderType != OrderType.DineIn
                || destinationOrder.OrderType != OrderType.DineIn
            )
            {
                return DomainResult<SplitOrderPlan>.Failure(
                    DomainErrors.Order.InvalidActionWithStatus
                );
            }

            var newDestinationItems = new List<OrderItem>();
            var deletedSourceItems = new List<OrderItem>();

            foreach (var splitRequest in splitRequests)
            {
                var sourceItem = OrderItems.First(item =>
                    item.OrderItemId == splitRequest.OrderItemId
                );

                if (!sourceItem.CanBeMoved())
                {
                    return DomainResult<SplitOrderPlan>.Failure(
                        DomainErrors.Order.InvalidActionWithStatus
                    );
                }

                if (
                    splitRequest.QuantityToSplit <= 0
                    || splitRequest.QuantityToSplit > sourceItem.Quantity
                )
                {
                    return DomainResult<SplitOrderPlan>.Failure(
                        DomainErrors.OrderItem.InvalidQuantity
                    );
                }

                if (splitRequest.QuantityToSplit == sourceItem.Quantity)
                {
                    var mergeTarget = destinationOrder.OrderItems.FirstOrDefault(item =>
                        item.HasSameConfiguration(sourceItem)
                    );

                    if (mergeTarget != null)
                    {
                        mergeTarget.IncreaseQuantity(sourceItem.Quantity, updatedAt);
                        OrderItems.Remove(sourceItem);
                        deletedSourceItems.Add(sourceItem);
                        continue;
                    }

                    var moveResult = sourceItem.MoveToOrder(destinationOrder.OrderId, updatedAt);
                    if (!moveResult.IsSuccess)
                    {
                        return DomainResult<SplitOrderPlan>.Failure(
                            moveResult.ErrorCode ?? DomainErrors.Order.InvalidActionWithStatus
                        );
                    }

                    OrderItems.Remove(sourceItem);
                    destinationOrder.OrderItems.Add(sourceItem);
                    continue;
                }

                var reduceResult = sourceItem.ReduceQuantity(
                    splitRequest.QuantityToSplit,
                    updatedAt
                );
                if (!reduceResult.IsSuccess)
                {
                    return DomainResult<SplitOrderPlan>.Failure(
                        reduceResult.ErrorCode ?? DomainErrors.OrderItem.InvalidQuantity
                    );
                }

                var clonedItem = sourceItem.CloneForOrder(
                    destinationOrder.OrderId,
                    splitRequest.QuantityToSplit,
                    updatedAt
                );

                var existingDestinationItem = destinationOrder.OrderItems.FirstOrDefault(item =>
                    item.HasSameConfiguration(clonedItem)
                );

                if (existingDestinationItem != null)
                {
                    existingDestinationItem.IncreaseQuantity(clonedItem.Quantity, updatedAt);
                    continue;
                }

                destinationOrder.OrderItems.Add(clonedItem);
                newDestinationItems.Add(clonedItem);
            }

            RecalculateTotalAmount();
            UpdatedAt = updatedAt;
            UpdatedBy = updatedBy;

            destinationOrder.RecalculateTotalAmount();
            destinationOrder.UpdatedAt = updatedAt;
            destinationOrder.UpdatedBy = updatedBy;

            if (!OrderItems.Any())
            {
                MarkAsClosed(updatedAt, updatedBy);
            }

            return DomainResult<SplitOrderPlan>.Success(
                new SplitOrderPlan(newDestinationItems, deletedSourceItems)
            );
        }

        public OrderItem CreateOrderItem(
            MenuItem menuItem,
            int quantity,
            string? note,
            List<(
                MenuItemOptionGroup Assignment,
                OptionGroup Group,
                List<(OptionItem Item, int Quantity, string? Note)> Selections
            )> options
        )
        {
            var newItem = new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = this.OrderId,
                MenuItemId = menuItem.MenuItemId,
                Quantity = quantity,
                ItemNote = note,
                CreatedAt = DateTime.UtcNow,
                Status = OrderItemStatus.Preparing,
                ItemNameSnapshot = menuItem.Name,
                ItemCodeSnapshot = menuItem.Code,
                UnitPriceSnapshot = menuItem.Price,
                StationSnapshot = menuItem.Station.ToString(),
            };

            foreach (var optGroup in options)
            {
                var orderItemOptGroup = new OrderItemOptionGroup
                {
                    OrderItemOptionGroupId = Guid.NewGuid(),
                    OrderItemId = newItem.OrderItemId,
                    GroupNameSnapshot = optGroup.Group.Name,
                    GroupTypeSnapshot = optGroup.Group.OptionType.ToString(),
                    IsRequiredSnapshot = optGroup.Assignment.IsRequired,
                    CreatedAt = DateTime.UtcNow,
                };

                foreach (var selection in optGroup.Selections)
                {
                    var orderItemOptValue = new OrderItemOptionValue
                    {
                        OrderItemOptionValueId = Guid.NewGuid(),
                        OrderItemOptionGroupId = orderItemOptGroup.OrderItemOptionGroupId,
                        OptionItemId = selection.Item.OptionItemId,
                        LabelSnapshot = selection.Item.Label,
                        ExtraPriceSnapshot = selection.Item.ExtraPrice,
                        Quantity = selection.Quantity,
                        Note = selection.Note,
                        CreatedAt = DateTime.UtcNow,
                    };
                    orderItemOptGroup.OptionValues.Add(orderItemOptValue);
                }
                newItem.OptionGroups.Add(orderItemOptGroup);
            }

            return newItem;
        }

        public (OrderItem Item, bool IsNew) AddOrUpdateItem(
            MenuItem menuItem,
            int quantity,
            string? note,
            List<(
                MenuItemOptionGroup Assignment,
                OptionGroup Group,
                List<(OptionItem Item, int Quantity, string? Note)> Selections
            )> options
        )
        {
            // Generate signature for matching logic
            var signature = GenerateSignature(options);

            // Try to find existing item to merge
            var existingItem = OrderItems.FirstOrDefault(oi =>
                oi.MenuItemId == menuItem.MenuItemId
                && oi.Status == OrderItemStatus.Preparing
                && (oi.ItemNote ?? string.Empty) == (note ?? string.Empty)
                && GetItemSignature(oi) == signature
            );

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
                RecalculateTotalAmount();
                return (existingItem, false);
            }

            // 3. Create new item
            var newItem = CreateOrderItem(menuItem, quantity, note, options);

            OrderItems.Add(newItem);
            RecalculateTotalAmount();
            return (newItem, true);
        }

        public string GenerateSignature(
            List<(
                MenuItemOptionGroup Assignment,
                OptionGroup Group,
                List<(OptionItem Item, int Quantity, string? Note)> Selections
            )> options
        )
        {
            if (options == null || !options.Any())
                return string.Empty;

            var allValues = options
                .SelectMany(og => og.Selections)
                .OrderBy(v => v.Item.OptionItemId)
                .Select(v => $"{v.Item.OptionItemId}x{v.Quantity}");

            return string.Join("|", allValues);
        }

        public string GetItemSignature(OrderItem item)
        {
            if (item.OptionGroups == null || !item.OptionGroups.Any())
                return string.Empty;

            var allValues = item
                .OptionGroups.SelectMany(og => og.OptionValues)
                .Where(ov => ov.OptionItemId.HasValue)
                .OrderBy(ov => ov.OptionItemId)
                .Select(ov => $"{ov.OptionItemId}x{ov.Quantity}");

            return string.Join("|", allValues);
        }

        // Vùng logic liên quan đến voucher - có thể phức tạp tùy theo yêu cầu kinh doanh, nên tách riêng để dễ quản lý và mở rộng sau này
        // Promotion logic
        public void ApplyPromotion(Promotion? promotion, Guid auditorId)
        {
            Promotion = promotion;
            PromotionId = promotion?.PromotionId;
            RecalculateTotalAmount();

            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = auditorId;
        }

        public decimal CalculateDiscount()
        {
            if (Promotion == null)
            {
                return 0;
            }

            // Using UTCNow to match Promotion's UTC comparison
            var validation = Promotion.Validate(
                GetPromotionValidationSubTotal(),
                DateTimeOffset.UtcNow
            );
            if (!validation.IsSuccess)
            {
                return 0;
            }

            return Promotion.Type switch
            {
                PromotionType.Percent => Math.Min(
                    SubTotal * Promotion.Value / 100,
                    Promotion.MaxDiscount ?? decimal.MaxValue
                ),
                PromotionType.Fixed => Math.Min(Promotion.Value, SubTotal),
                PromotionType.FreeItem => 0, // Logic handled at item level if needed
                _ => 0,
            };
        }
        // Kết thúc vùng logic của voucher
    }

    public sealed record MergeOrderPlan(IReadOnlyCollection<OrderItem> DeletedSourceItems);

    public sealed record OrderItemSplitRequest(Guid OrderItemId, int QuantityToSplit);

    public sealed record SplitOrderPlan(
        IReadOnlyCollection<OrderItem> NewDestinationItems,
        IReadOnlyCollection<OrderItem> DeletedSourceItems
    );
}
