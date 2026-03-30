using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.PaymentMethods.Queries.GetPaymentMethodById
{
    public class GetPaymentMethodByIdHandler
        : IRequestHandler<GetPaymentMethodByIdQuery, Result<GetPaymentMethodByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPaymentMethodByIdHandler> _logger;
        private readonly IMessageService _messageService;

        public GetPaymentMethodByIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetPaymentMethodByIdHandler> logger,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<GetPaymentMethodByIdResponse>> Handle(
            GetPaymentMethodByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting PaymentMethodConfig by Id: {Id}", request.PaymentMethodConfigId);

            var entity = await _unitOfWork.Repository<PaymentMethodConfig>()
                .Query()
                .FirstOrDefaultAsync(p => p.PaymentMethodConfigId == request.PaymentMethodConfigId, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("PaymentMethodConfig not found: {Id}", request.PaymentMethodConfigId);
                return Result<GetPaymentMethodByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.PaymentMethodConfig.NotFound),
                    ResultErrorType.NotFound);
            }

            var response = _mapper.Map<GetPaymentMethodByIdResponse>(entity);
            return Result<GetPaymentMethodByIdResponse>.Success(response);
        }
    }
}
