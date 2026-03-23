using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using MediatR;

namespace FoodHub.Application.Features.Billing.Commands.CreateQrPayment
{
    public class CreateQrPaymentCommand : IRequest<Result<PaymentLinkResponse>>
    {
        public Guid OrderId { get; set; }
    }
}
