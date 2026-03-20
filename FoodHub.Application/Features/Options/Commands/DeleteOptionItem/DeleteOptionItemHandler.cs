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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Options.Commands.DeleteOptionItem
{
    public class DeleteOptionItemHandler
        : IRequestHandler<DeleteOptionItemCommand, Result<DeleteOptionItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<DeleteOptionItemHandler> _logger;

        public DeleteOptionItemHandler(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<DeleteOptionItemHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<DeleteOptionItemResponse>> Handle(
            DeleteOptionItemCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start deleting option item OptionItemId={OptionItemId}",
                request.OptionItemId
            );
            var repo = _unitOfWork.Repository<OptionItem>();

            var optionItem = await repo.Query()
                .FirstOrDefaultAsync(
                    oi => oi.OptionItemId == request.OptionItemId,
                    cancellationToken
                );

            if (optionItem is null)
                return Result<DeleteOptionItemResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.OptionItem.NotFound)
                );

            optionItem.DeletedAt = DateTime.UtcNow;
            optionItem.UpdatedAt = DateTime.UtcNow;
            optionItem.UpdatedBy = Guid.TryParse(_currentUserService.UserId, out var userId)
                ? userId
                : null;

            await _unitOfWork.SaveChangeAsync();

            await _cacheService.RemoveByPatternAsync(
                CacheKey.OptionReusableList,
                cancellationToken
            );
            await _cacheService.RemoveByPatternAsync("option:menuitem:", cancellationToken);

            _logger.LogInformation(
                "End deleting option item OptionItemId={OptionItemId}",
                optionItem.OptionItemId
            );

            return Result<DeleteOptionItemResponse>.Success(
                new DeleteOptionItemResponse(optionItem.OptionItemId, optionItem.DeletedAt)
            );
        }
    }
}
