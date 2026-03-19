using FoodHub.Application.Common.Models;
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

namespace FoodHub.Application.Features.Options.Commands.CreateOptionItem
{
    public class CreateOptionItemHandler
        : IRequestHandler<CreateOptionItemCommand, Result<CreateOptionItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateOptionItemHandler> _logger;
        private readonly IMessageService _messageService;

        public CreateOptionItemHandler(
            IUnitOfWork unitOfWork,
            ILogger<CreateOptionItemHandler> logger,
            IMessageService messageService
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<CreateOptionItemResponse>> Handle(
            CreateOptionItemCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start creating option item Label={Label} for OptionGroupId={OptionGroupId}",
                request.Label,
                request.OptionGroupId
            );
            var optionGroupRepository = _unitOfWork.Repository<OptionGroup>();
            var optionGroup = await optionGroupRepository.GetByIdAsync(request.OptionGroupId);
            if (optionGroup == null)
            {
                return Result<CreateOptionItemResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.OptionGroup.NotFound, request.OptionGroupId)
                );
            }

            var optionItem = OptionItem.Create(
                request.OptionGroupId,
                request.Label,
                request.ExtraPrice
            );

            await _unitOfWork.Repository<OptionItem>().AddAsync(optionItem);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new CreateOptionItemResponse
            {
                OptionItemId = optionItem.OptionItemId,
                OptionGroupId = optionItem.OptionGroupId,
                Label = optionItem.Label,
                ExtraPrice = optionItem.ExtraPrice,
            };

            _logger.LogInformation(
                "End creating option item OptionItemId={OptionItemId}",
                optionItem.OptionItemId
            );

            return Result<CreateOptionItemResponse>.Success(response);
        }
    }
}
