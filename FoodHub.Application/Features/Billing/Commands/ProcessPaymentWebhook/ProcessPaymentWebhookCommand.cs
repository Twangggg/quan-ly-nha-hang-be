using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Billing.Commands.ProcessPaymentWebhook
{
    public class ProcessPaymentWebhookCommand : IRequest<Result<bool>>
    {
        public string WebhookBody { get; set; } = string.Empty;
    }
}
