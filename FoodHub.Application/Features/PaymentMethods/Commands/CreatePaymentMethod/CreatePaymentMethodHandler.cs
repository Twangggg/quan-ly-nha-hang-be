using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.PaymentMethods.Commands.CreatePaymentMethod
{
    public class CreatePaymentMethodHandler
        : IRequestHandler<CreatePaymentMethodCommand, Result<CreatePaymentMethodResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CreatePaymentMethodHandler> _logger;
        private readonly IMessageService _messageService;

        public CreatePaymentMethodHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CreatePaymentMethodHandler> logger,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<CreatePaymentMethodResponse>> Handle(
            CreatePaymentMethodCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating PaymentMethodConfig: {Name}", request.Name);

            // Check duplicate name
            var exists = await _unitOfWork.Repository<PaymentMethodConfig>()
                .Query()
                .AnyAsync(p => p.Name == request.Name, cancellationToken);

            if (exists)
            {
                _logger.LogWarning("PaymentMethodConfig name already exists: {Name}", request.Name);
                return Result<CreatePaymentMethodResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.PaymentMethodConfig.NameDuplicate),
                    ResultErrorType.BadRequest);
            }

            var entity = new PaymentMethodConfig
            {
                PaymentMethodConfigId = Guid.NewGuid(),
                IsActive = true,
                IsDefault = false,
            };

            var domainResult = entity.UpdateInfo(
                request.Name,
                request.Type);

            if (!domainResult.IsSuccess)
            {
                _logger.LogWarning("PaymentMethodConfig validation failed: {Error}", domainResult.ErrorCode);
                return Result<CreatePaymentMethodResponse>.Failure(
                    _messageService.GetMessage(domainResult.ErrorCode ?? MessageKeys.Common.ValidationFailed),
                    ResultErrorType.BadRequest);
            }

            await _unitOfWork.Repository<PaymentMethodConfig>().AddAsync(entity);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation("PaymentMethodConfig created: {Id}", entity.PaymentMethodConfigId);

            var response = _mapper.Map<CreatePaymentMethodResponse>(entity);
            return Result<CreatePaymentMethodResponse>.Success(response);
        }
    }
}
