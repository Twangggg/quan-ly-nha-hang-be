using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FoodHub.Application.Constants;

namespace FoodHub.Application.Features.Authentication.Commands.RevokeToken
{
    public class RevokeTokenHandler : IRequestHandler<RevokeTokenCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;

        public RevokeTokenHandler(IUnitOfWork unitOfWork, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
        }

        public async Task<Result<bool>> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
        {
            var token = await _unitOfWork.Repository<FoodHub.Domain.Entities.RefreshToken>()
                .Query()
                .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

            if (token == null)
            {
                return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Auth.InvalidToken));
            }

            _unitOfWork.Repository<FoodHub.Domain.Entities.RefreshToken>().Delete(token);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}
