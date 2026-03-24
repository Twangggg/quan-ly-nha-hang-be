using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Vouchers.Commands.UpdateVoucher
{
    public class UpdateVoucherHandler : IRequestHandler<UpdateVoucherCommand, Result<UpdateVoucherResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateVoucherHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public UpdateVoucherHandler(
            IUnitOfWork unitOfWork,
            ILogger<UpdateVoucherHandler> logger,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            IMapper mapper,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _cacheService = cacheService;
        }
        public async Task<Result<UpdateVoucherResponse>> Handle(UpdateVoucherCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling UpdateVoucherCommand for VoucherId: {VoucherId}", request.VoucherId);
            Guid? auditorId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var parsedId))
            {
                _logger.LogInformation("Current user ID parsed successfully: {UserId}", parsedId);
                auditorId = parsedId;
            }

            var voucherRepository = _unitOfWork.Repository<Voucher>();
            var menuItemRepository = _unitOfWork.Repository<MenuItem>();

            var voucher = await voucherRepository
                .Query()
                .Include(v => v.Item)
                .FirstOrDefaultAsync(v => v.VoucherId == request.VoucherId, cancellationToken);
            if (voucher == null)
            {
                _logger.LogWarning("Voucher with ID {VoucherId} not found", request.VoucherId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Voucher.NotFound);
                return Result<UpdateVoucherResponse>.NotFound(errorMessage);
            }

            if (voucher.VoucherCode != request.VoucherCode
                && await voucherRepository
                .Query()
                .AnyAsync(v => v.VoucherCode == request.VoucherCode, cancellationToken))
            {
                _logger.LogWarning("Voucher code {VoucherCode} already exists", request.VoucherCode);
                var errorMessage = _messageService.GetMessage(MessageKeys.Voucher.CodeAlreadyExists);
                return Result<UpdateVoucherResponse>.Failure(errorMessage);
            }

            var menuItem = await menuItemRepository
                .Query()
                .FirstOrDefaultAsync(m => m.MenuItemId == request.ItemtId, cancellationToken);
            if (request.ItemtId.HasValue && request.VoucherType == VoucherType.FreeItem)
            {

                if (menuItem == null)
                {
                    _logger.LogWarning("Menu item with ID {ItemId} not found", request.ItemtId.Value);
                    var errorMessage = _messageService.GetMessage(MessageKeys.MenuItem.NotFound);
                    return Result<UpdateVoucherResponse>.NotFound(errorMessage);
                }
            }

            voucher.VoucherCode = request.VoucherCode.ToUpper();
            voucher.VoucherType = request.VoucherType;
            voucher.DiscountValue = request.DiscountValue;
            voucher.MaxDiscount = request.MaxDiscount;
            voucher.MinOrderValue = request.MinOrderValue;
            voucher.ItemId = request.ItemtId;
            voucher.FreeQuantity = request.FreeQuantity;
            voucher.StartDate = request.StartDate;
            voucher.EndDate = request.EndDate;
            voucher.StartTime = request.StartTime;
            voucher.EndTime = request.EndTime;
            voucher.IsActive = request.IsActive;
            voucher.UsageLimit = request.UsageLimit;

            voucher.UpdatedAt = DateTime.UtcNow;
            voucher.UpdatedBy = auditorId;

            voucherRepository.Update(voucher);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation("Voucher with ID {VoucherId} updated successfully", voucher.VoucherId);

            await _cacheService.RemoveByPatternAsync(CacheKey.VoucherList, cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.VoucherById, voucher.VoucherId), cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.VoucherByCode, voucher.VoucherCode), cancellationToken);

            var response = _mapper.Map<UpdateVoucherResponse>(voucher);
            return Result<UpdateVoucherResponse>.Success(response);
        }
    }
}
