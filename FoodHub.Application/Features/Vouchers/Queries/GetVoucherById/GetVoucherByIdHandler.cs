using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Vouchers.Queries.GetVoucherById
{
    public class GetVoucherByIdHandler : IRequestHandler<GetVoucherByIdQuery, Result<GetVoucherByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;
        private readonly ILogger<GetVoucherByIdHandler> _logger;
        private readonly IMessageService _messageService;

        public GetVoucherByIdHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IMapper mapper,
            ILogger<GetVoucherByIdHandler> logger,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _mapper = mapper;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<GetVoucherByIdResponse>> Handle(GetVoucherByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetVoucherByIdQuery for VoucherId: {VoucherId}", request.VoucherId);
            var cacheKey = string.Format(CacheKey.VoucherById, request.VoucherId);
            var cachedVoucher = await _cacheService.GetAsync<GetVoucherByIdResponse>(cacheKey, cancellationToken);
            if (cachedVoucher != null)
            {
                _logger.LogInformation("Cache hit for VoucherId: {VoucherId}", request.VoucherId);
                return Result<GetVoucherByIdResponse>.Success(cachedVoucher);
            }

            var voucherRepository = _unitOfWork.Repository<Voucher>();

            var voucher = await voucherRepository
                .Query()
                .FirstOrDefaultAsync(v => v.VoucherId == request.VoucherId, cancellationToken);
            if (voucher == null)
            {
                _logger.LogWarning("Voucher with ID {VoucherId} not found", request.VoucherId);
                var errorMessage = _messageService.GetMessage(MessageKeys.Voucher.NotFound, request.VoucherId);
                return Result<GetVoucherByIdResponse>.NotFound(errorMessage);
            }

            _logger.LogInformation("Voucher with ID {VoucherId} found, mapping to response", request.VoucherId);
            var response = _mapper.Map<GetVoucherByIdResponse>(voucher);
            await _cacheService.SetAsync(cacheKey, response, CacheTTL.Vouchers, cancellationToken);
            _logger.LogInformation("Voucher with ID {VoucherId} cached with key {CacheKey}", request.VoucherId, cacheKey);
            return Result<GetVoucherByIdResponse>.Success(response);
        }
    }
}
