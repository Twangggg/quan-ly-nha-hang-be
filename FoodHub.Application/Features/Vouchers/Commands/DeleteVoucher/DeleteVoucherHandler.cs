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

namespace FoodHub.Application.Features.Vouchers.Commands.DeleteVoucher
{
    public class DeleteVoucherHandler : IRequestHandler<DeleteVoucherCommand, Result<DeleteVoucherResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<DeleteVoucherHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;

        public DeleteVoucherHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<DeleteVoucherHandler> logger,
            ICurrentUserService currentUserService,
            ICacheService cacheService,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
        }
        public async Task<Result<DeleteVoucherResponse>> Handle(DeleteVoucherCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling DeleteVoucherCommand for VoucherId: {VoucherId} by User: {UserId}", request.VoucherId, _currentUserService.UserId);

            var auditorId = _currentUserService.GetRequiredUserIdAsGuid();

            var voucherRepository = _unitOfWork.Repository<Voucher>();

            var voucher = await voucherRepository
                .Query()
                .FirstOrDefaultAsync(v => v.VoucherId == request.VoucherId, cancellationToken);
            if (voucher == null)
            {
                _logger.LogWarning("Voucher with Id: {VoucherId} not found for deletion", request.VoucherId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Voucher.NotFound, request.VoucherId);
                return Result<DeleteVoucherResponse>.NotFound(errorMessage);
            }

            voucher.DeletedAt = DateTime.UtcNow;
            voucher.UpdatedBy = auditorId;

            voucherRepository.Update(voucher);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation("Voucher with Id: {VoucherId} deleted by User: {UserId}", request.VoucherId, auditorId);

            await _cacheService.RemoveAsync(CacheKey.VoucherList, cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.VoucherById, request.VoucherId), cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.VoucherByCode, voucher.VoucherCode), cancellationToken);

            var response = _mapper.Map<DeleteVoucherResponse>(voucher);
            return Result<DeleteVoucherResponse>.Success(response);
        }
    }
}
