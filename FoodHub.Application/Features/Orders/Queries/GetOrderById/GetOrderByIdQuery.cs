using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.Orders.Queries.GetOrderById
{
    public record GetOrderByIdQuery : IRequest<Result<GetOrderByIdResponse>>
    {
        /// <summary>
        /// The unique identifier of the order.
        /// </summary>
        public Guid OrderId { get; set; }
    }

    public class GetOrderByIdValidator : AbstractValidator<GetOrderByIdQuery>
    {
        public GetOrderByIdValidator(IMessageService messageService)
        {
            RuleFor(x => x.OrderId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Common.IdRequired));
        }
    }
}
