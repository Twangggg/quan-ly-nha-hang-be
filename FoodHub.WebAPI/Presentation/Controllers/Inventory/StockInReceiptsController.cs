using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Inventory.StockInReceipts.Commands.CreateStockInReceipt;
using FoodHub.Application.Features.Inventory.StockInReceipts.Commands.ReverseStockInReceipt;
using FoodHub.Application.Features.Inventory.StockInReceipts.Queries.GetStockInReceiptById;
using FoodHub.Application.Features.Inventory.StockInReceipts.Queries.GetStockInReceipts;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
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

        /// <summary>
        /// Lấy danh sách phiếu nhập kho (phân trang, lọc theo ngày, tìm kiếm mã phiếu).
        /// </summary>
        /// <param name="pagination">Tham số phân trang và tìm kiếm (PageNumber, PageSize, Search).</param>
        /// <param name="fromDate">Ngày nhận hàng bắt đầu (UTC, bao gồm).</param>
        /// <param name="toDate">Ngày nhận hàng kết thúc (UTC, bao gồm).</param>
        /// <response code="200">Danh sách phiếu nhập kho kèm thông tin phân trang.</response>
        [HttpGet("/api/v{version:apiVersion}/inventory/stock-in")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetStockInReceiptsResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetStockInReceipts(
            [FromQuery] PaginationParams pagination,
            [FromQuery] DateOnly? fromDate,
            [FromQuery] DateOnly? toDate
        )
        {
            var result = await _mediator.Send(
                new GetStockInReceiptsQuery(pagination, fromDate, toDate)
            );
            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy chi tiết phiếu nhập kho theo ID.
        /// </summary>
        /// <param name="id">ID phiếu nhập kho.</param>
        /// <response code="200">Thông tin chi tiết phiếu nhập kho.</response>
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

        /// <summary>
        /// Tạo mới phiếu nhập kho.
        /// </summary>
        /// <param name="command">Thông tin phiếu nhập kho (nhà cung cấp, nguyên liệu, số lượng... ).</param>
        /// <response code="201">Tạo phiếu nhập kho thành công.</response>
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
                data =>
                    Url.Action(nameof(GetStockInReceiptById), new { id = data.StockInReceiptId })
            );
        }

        /// <summary>
        /// Hoàn nhập (reverse) phiếu nhập kho đã tạo.
        /// </summary>
        /// <param name="id">ID phiếu nhập kho cần hoàn.</param>
        /// <response code="200">Hoàn nhập phiếu nhập kho thành công.</response>
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
