using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionItem
{
    public class UpdateOptionItemHandler
        : IRequestHandler<UpdateOptionItemCommand, Result<UpdateOptionItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ILogger<UpdateOptionItemHandler> _logger;
        private readonly IMessageService _messageService;

        public UpdateOptionItemHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ILogger<UpdateOptionItemHandler> logger,
            IMessageService messageService
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<UpdateOptionItemResponse>> Handle(
            UpdateOptionItemCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start updating option item OptionItemId={OptionItemId} Label={Label}",
                request.OptionItemId,
                request.Label
            );
            var optionItem = await _unitOfWork
                .Repository<OptionItem>()
                .GetByIdAsync(request.OptionItemId);

            if (optionItem == null)
            {
                return Result<UpdateOptionItemResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.OptionItem.NotFound, request.OptionItemId)
                );
            }

            optionItem.Update(request.Label, request.ExtraPrice);

            _unitOfWork.Repository<OptionItem>().Update(optionItem);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _cacheService.RemoveByPatternAsync(
                CacheKey.OptionReusableList,
                cancellationToken
            );
            await _cacheService.RemoveByPatternAsync("option:menuitem:", cancellationToken);

            var response = new UpdateOptionItemResponse
            {
                OptionItemId = optionItem.OptionItemId,
                OptionGroupId = optionItem.OptionGroupId,
                Label = optionItem.Label,
                ExtraPrice = optionItem.ExtraPrice,
            };

            _logger.LogInformation(
                "End updating option item OptionItemId={OptionItemId}",
                optionItem.OptionItemId
            );

            return Result<UpdateOptionItemResponse>.Success(response);
        }
    }
}
