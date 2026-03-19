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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Options.Commands.DeleteOptionGroup
{
    public class DeleteOptionGroupHandler
        : IRequestHandler<DeleteOptionGroupCommand, Result<DeleteOptionGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<DeleteOptionGroupHandler> _logger;

        public DeleteOptionGroupHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<DeleteOptionGroupHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<DeleteOptionGroupResponse>> Handle(
            DeleteOptionGroupCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start deleting option group OptionGroupId={OptionGroupId}",
                request.OptionGroupId
            );
            var repo = _unitOfWork.Repository<OptionGroup>();

            var optionGroup = await repo.Query()
                .FirstOrDefaultAsync(
                    og => og.OptionGroupId == request.OptionGroupId,
                    cancellationToken
                );

            if (optionGroup is null)
                return Result<DeleteOptionGroupResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.OptionGroup.NotFound)
                );

            optionGroup.DeletedAt = DateTime.UtcNow;
            optionGroup.UpdatedAt = DateTime.UtcNow;
            optionGroup.UpdatedBy = Guid.TryParse(_currentUserService.UserId, out var userId)
                ? userId
                : null;

            await _unitOfWork.SaveChangeAsync();

            _logger.LogInformation(
                "End deleting option group OptionGroupId={OptionGroupId}",
                optionGroup.OptionGroupId
            );

            return Result<DeleteOptionGroupResponse>.Success(
                new DeleteOptionGroupResponse(optionGroup.OptionGroupId, optionGroup.DeletedAt)
            );
        }
    }
}
