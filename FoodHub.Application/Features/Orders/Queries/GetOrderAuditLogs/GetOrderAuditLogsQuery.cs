using FluentValidation;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using MediatR;

namespace FoodHub.Application.Features.Orders.Queries.GetOrderAuditLogs
{
    public record GetOrderAuditLogsQuery(Guid OrderId, PaginationParams Pagination)
        : IRequest<Result<PagedResult<GetOrderAuditLogsResponse>>>;

    public class GetOrderAuditLogsValidator : AbstractValidator<GetOrderAuditLogsQuery>
    {
        public GetOrderAuditLogsValidator(IMessageService messageService)
        {
            RuleFor(x => x.OrderId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));
        }
    }
}
