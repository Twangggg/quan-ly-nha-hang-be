using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Vouchers.Commands.CreateVoucher
{
    public class CreateVoucherHandler : IRequestHandler<CreateVoucherCommand, Result<CreateVoucherResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateVoucherHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public CreateVoucherHandler(
            IUnitOfWork unitOfWork,
            ILogger<CreateVoucherHandler> logger,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            IMapper mapper,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
            _mapper = mapper;
        }

        public async Task<Result<CreateVoucherResponse>> Handle(CreateVoucherCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling CreateVoucherCommand for voucher code: {VoucherCode}", request.VoucherCode);

            // Attempt to parse the current user's ID for auditing purposes
            Guid? auditorId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var parsedId))
            {
                _logger.LogInformation("Current user ID parsed successfully: {UserId}", parsedId);
                auditorId = parsedId;
            }

            // Get the necessary repositories for vouchers and menu items
            var voucherRepository = _unitOfWork.Repository<Voucher>();
            var menuItemRepository = _unitOfWork.Repository<MenuItem>();

            // Validate that the voucher code is unique
            if (await voucherRepository.Query().AnyAsync(v => v.VoucherCode == request.VoucherCode))
            {
                _logger.LogWarning("Voucher code already exists: {VoucherCode}", request.VoucherCode);
                var errorMessage = _messageService.GetMessage(MessageKeys.Voucher.CodeAlreadyExists);
                return Result<CreateVoucherResponse>.Failure(errorMessage, ResultErrorType.Conflict);
            }

            // If an ItemtId is provided, validate that the corresponding menu item exists
            var menuItem = await menuItemRepository
                .Query()
                .FirstOrDefaultAsync(m => m.MenuItemId == request.ItemtId);
            if (request.ItemtId.HasValue && menuItem == null)
            {
                _logger.LogWarning("Menu item not found for ID: {MenuItemId}", request.ItemtId);
                var errorMessage = _messageService.GetMessage(MessageKeys.MenuItem.NotFound);
                return Result<CreateVoucherResponse>.Failure(errorMessage, ResultErrorType.BadRequest);
            }

            // Create a new voucher entity based on the request data
            var voucher = new Voucher
            {
                VoucherId = Guid.NewGuid(),
                VoucherCode = request.VoucherCode.ToUpper(),
                VoucherType = request.VoucherType,
                DiscountValue = request.DiscountValue,
                MaxDiscount = request.MaxDiscount,
                MinOrderValue = request.MinOrderValue,
                ItemId = request.ItemtId,
                FreeQuantity = request.FreeQuantity,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                IsActive = request.IsActive,
                UsageLimit = request.UsageLimit,
                UsedCount = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = auditorId,
            };

            // Save the new voucher to the database
            await voucherRepository.AddAsync(voucher);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Log the successful creation of the voucher
            _logger.LogInformation("Voucher created successfully with ID: {VoucherId}", voucher.VoucherId);

            // Invalidate relevant cache entries
            await _cacheService.RemoveAsync(CacheKey.VoucherList, cancellationToken);

            // Map the created voucher to the response DTO
            var response = _mapper.Map<CreateVoucherResponse>(voucher);
            return Result<CreateVoucherResponse>.Success(response);
        }
    }
}
