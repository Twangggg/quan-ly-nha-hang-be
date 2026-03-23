using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Billing.Commands.SplitBill
{
    public class SplitBillCommand : IRequest<Result<SplitBillResponse>>
    {
        public Guid OrderId { get; set; }
        public List<SplitBillItemCommand> ItemsToSplit { get; set; } = new();
    }

    public class SplitBillItemCommand
    {
        public Guid OrderItemId { get; set; }
        public int QuantityToSplit { get; set; }
    }
}
