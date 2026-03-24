using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Vouchers.Commands.UpdateVoucherActive
{
    public class UpdateVoucherActiveHandler : IRequestHandler<UpdateVoucherActiveCommand, Result<UpdateVoucherActiveResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateVoucherActiveHandler> _logger;
        private readonly IMessageService _messageService;

        public UpdateVoucherActiveHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            ICacheService cacheService,
            IMapper mapper,
            ILogger<UpdateVoucherActiveHandler> logger,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
            _mapper = mapper;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<UpdateVoucherActiveResponse>> Handle(UpdateVoucherActiveCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling UpdateVoucherActiveCommand for VoucherId: {VoucherId}", request.VoucherId);

            var auditorId = _currentUserService.GetRequiredUserIdAsGuid();

            var voucherRepository = _unitOfWork.Repository<Voucher>();

            var voucher = await voucherRepository
                .Query()
                .FirstOrDefaultAsync(v => v.VoucherId == request.VoucherId, cancellationToken);
            if (voucher == null)
            {
                _logger.LogWarning("Voucher with ID {VoucherId} not found", request.VoucherId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Voucher.NotFound, request.VoucherId);
                return Result<UpdateVoucherActiveResponse>.NotFound(errorMessage);
            }

            voucher.IsActive = request.IsActive;
            voucher.UpdatedAt = DateTime.UtcNow;
            voucher.UpdatedBy = auditorId;

            voucherRepository.Update(voucher);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation("Voucher with ID {VoucherId} updated successfully. IsActive: {IsActive}", request.VoucherId, request.IsActive);

            await _cacheService.RemoveByPatternAsync(CacheKey.VoucherList, cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.VoucherById, voucher.VoucherId));
            await _cacheService.RemoveAsync(string.Format(CacheKey.VoucherByCode, voucher.VoucherCode));

            var response = _mapper.Map<UpdateVoucherActiveResponse>(voucher);
            return Result<UpdateVoucherActiveResponse>.Success(response);

        }
    }
}
