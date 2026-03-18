using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Authentication.Commands.RevokeToken
{
    public class RevokeTokenHandler : IRequestHandler<RevokeTokenCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IAuditLogService _auditLogService;

        public RevokeTokenHandler(IUnitOfWork unitOfWork, IMessageService messageService, IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _auditLogService = auditLogService;
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

            token.IsRevoked = true;
            token.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<FoodHub.Domain.Entities.RefreshToken>().Update(token);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _auditLogService.LogActivityAsync(AuditAction.Logout, "Auth", token.EmployeeId.ToString());

            return Result<bool>.Success(true);
        }
    }
}
