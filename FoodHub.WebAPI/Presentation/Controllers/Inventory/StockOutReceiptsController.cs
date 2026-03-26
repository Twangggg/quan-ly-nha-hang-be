using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Inventory.StockOutReceipts.Commands.CreateStockOutReceipt;
using FoodHub.Application.Features.Inventory.StockOutReceipts.Commands.ReverseStockOutReceipt;
using FoodHub.Application.Features.Inventory.StockOutReceipts.Queries.GetStockOutReceiptById;
using FoodHub.Application.Features.Inventory.StockOutReceipts.Queries.GetStockOutReceipts;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
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

        public StockOutReceiptsController(IMediator mediator, IMessageService messageService) : base(messageService)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách phiếu xuất kho (phân trang, lọc theo ngày, tìm kiếm mã phiếu).
        /// </summary>
        /// <param name="pagination">Tham số phân trang và tìm kiếm (PageNumber, PageSize, Search).</param>
        /// <param name="fromDate">Ngày xuất kho bắt đầu (UTC, bao gồm).</param>
        /// <param name="toDate">Ngày xuất kho kết thúc (UTC, bao gồm).</param>
        /// <response code="200">Danh sách phiếu xuất kho kèm thông tin phân trang.</response>
        [HttpGet("/api/v{version:apiVersion}/inventory/stock-out")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetStockOutReceiptsResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetStockOutReceipts(
            [FromQuery] PaginationParams pagination,
            [FromQuery] DateOnly? fromDate,
            [FromQuery] DateOnly? toDate
        )
        {
            var result = await _mediator.Send(new GetStockOutReceiptsQuery(pagination, fromDate, toDate));
            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy chi tiết phiếu xuất kho theo ID.
        /// </summary>
        /// <param name="id">ID phiếu xuất kho.</param>
        /// <response code="200">Thông tin chi tiết phiếu xuất kho.</response>
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

        /// <summary>
        /// Tạo mới phiếu xuất kho.
        /// </summary>
        /// <param name="command">Thông tin phiếu xuất kho (nguyên liệu, số lượng, lý do...).</param>
        /// <response code="201">Tạo phiếu xuất kho thành công.</response>
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

        /// <summary>
        /// Hoàn (reverse) một phiếu xuất kho.
        /// </summary>
        /// <param name="id">ID phiếu xuất kho cần hoàn.</param>
        /// <response code="200">Hoàn phiếu xuất kho thành công.</response>
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
