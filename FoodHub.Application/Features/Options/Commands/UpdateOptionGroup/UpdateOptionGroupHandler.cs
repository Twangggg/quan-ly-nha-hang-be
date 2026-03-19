using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Options.Commands.UpdateOptionGroup
{
    public class UpdateOptionGroupHandler
        : IRequestHandler<UpdateOptionGroupCommand, Result<UpdateOptionGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateOptionGroupHandler> _logger;

        public UpdateOptionGroupHandler(
            IUnitOfWork unitOfWork,
            ILogger<UpdateOptionGroupHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<UpdateOptionGroupResponse>> Handle(
            UpdateOptionGroupCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start updating option group OptionGroupId={OptionGroupId}",
                request.OptionGroupId
            );

            var optionGroup = await _unitOfWork
                .Repository<OptionGroup>()
                .Query()
                .Include(og => og.OptionItems)
                .FirstOrDefaultAsync(
                    og => og.OptionGroupId == request.OptionGroupId,
                    cancellationToken
                );

            if (optionGroup == null)
            {
                throw new NotFoundException(
                    $"Option group with ID {request.OptionGroupId} not found."
                );
            }

            optionGroup.Update(request.Name, request.Type, request.IsRequired);

            _unitOfWork.Repository<OptionGroup>().Update(optionGroup);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = new UpdateOptionGroupResponse
            {
                OptionGroupId = optionGroup.OptionGroupId,
                MenuItemId = optionGroup.MenuItemId,
                Name = optionGroup.Name,
                Type = (int)optionGroup.OptionType,
                IsRequired = optionGroup.IsRequired,
            };

            _logger.LogInformation(
                "End updating option group OptionGroupId={OptionGroupId}",
                optionGroup.OptionGroupId
            );

            return Result<UpdateOptionGroupResponse>.Success(response);
        }
    }
}
