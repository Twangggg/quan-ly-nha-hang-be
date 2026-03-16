using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Inventory.StockInReceipts.Commands.CreateStockInReceipt;
using FoodHub.Application.Features.Inventory.StockInReceipts.Commands.ReverseStockInReceipt;
using FoodHub.Application.Features.Inventory.StockInReceipts.Queries.GetStockInReceiptById;
using FoodHub.Application.Features.Inventory.StockInReceipts.Queries.GetStockInReceipts;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý phiếu nhập kho và hoàn nhập.
    /// </summary>
    [Tags("Kho hang - Phieu nhap kho")]
    public class StockInReceiptsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public StockInReceiptsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("/api/v{version:apiVersion}/inventory/stock-in")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetStockInReceiptsResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetStockInReceipts([FromQuery] GetStockInReceiptsQuery query)
        {
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        [HttpGet("/api/v{version:apiVersion}/inventory/stock-in/{id:guid}")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<GetStockInReceiptByIdResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetStockInReceiptById(Guid id)
        {
            var result = await _mediator.Send(new GetStockInReceiptByIdQuery(id));
            return HandleResult(result);
        }

        [HttpPost("/api/v{version:apiVersion}/inventory/stock-in")]
        [HasPermission(Permissions.Inventory.Create)]
        [ProducesResponseType(
            typeof(Result<CreateStockInReceiptResponse>),
            StatusCodes.Status201Created
        )]
        public async Task<IActionResult> CreateStockInReceipt(
            [FromBody] CreateStockInReceiptCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleCreated(
                result,
                data => Url.Action(nameof(GetStockInReceiptById), new { id = data.StockInReceiptId })
            );
        }

        [HttpDelete("/api/v{version:apiVersion}/inventory/stock-in/{id:guid}")]
        [HasPermission(Permissions.Inventory.Update)]
        [ProducesResponseType(
            typeof(Result<ReverseStockInReceiptResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> ReverseStockInReceipt(Guid id)
        {
            var result = await _mediator.Send(new ReverseStockInReceiptCommand(id));
            return HandleResult(result);
        }
    }
}
