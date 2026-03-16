using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Inventory.StockOutReceipts.Commands.CreateStockOutReceipt;
using FoodHub.Application.Features.Inventory.StockOutReceipts.Commands.ReverseStockOutReceipt;
using FoodHub.Application.Features.Inventory.StockOutReceipts.Queries.GetStockOutReceiptById;
using FoodHub.Application.Features.Inventory.StockOutReceipts.Queries.GetStockOutReceipts;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý phiếu xuất kho và hoàn nhập xuất kho.
    /// </summary>
    [Tags("Kho hang - Phieu xuat kho")]
    public class StockOutReceiptsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public StockOutReceiptsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("/api/v{version:apiVersion}/inventory/stock-out")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetStockOutReceiptsResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetStockOutReceipts([FromQuery] GetStockOutReceiptsQuery query)
        {
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        [HttpGet("/api/v{version:apiVersion}/inventory/stock-out/{id:guid}")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<GetStockOutReceiptByIdResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetStockOutReceiptById(Guid id)
        {
            var result = await _mediator.Send(new GetStockOutReceiptByIdQuery(id));
            return HandleResult(result);
        }

        [HttpPost("/api/v{version:apiVersion}/inventory/stock-out")]
        [HasPermission(Permissions.Inventory.Create)]
        [ProducesResponseType(
            typeof(Result<CreateStockOutReceiptResponse>),
            StatusCodes.Status201Created
        )]
        public async Task<IActionResult> CreateStockOutReceipt(
            [FromBody] CreateStockOutReceiptCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleCreated(
                result,
                data => Url.Action(nameof(GetStockOutReceiptById), new { id = data.StockOutReceiptId })
            );
        }

        [HttpDelete("/api/v{version:apiVersion}/inventory/stock-out/{id:guid}")]
        [HasPermission(Permissions.Inventory.Update)]
        [ProducesResponseType(
            typeof(Result<ReverseStockOutReceiptResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> ReverseStockOutReceipt(Guid id)
        {
            var result = await _mediator.Send(new ReverseStockOutReceiptCommand(id));
            return HandleResult(result);
        }
    }
}
