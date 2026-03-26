using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Inventory.OpeningStock.Commands.ImportOpeningStock;
using FoodHub.Application.Features.Inventory.OpeningStock.Queries.GetOpeningStockList;
using FoodHub.Application.Features.Inventory.Settings.Commands.UpdateInventorySettings;
using FoodHub.Application.Features.Inventory.Settings.Queries.GetInventorySettings;
using FoodHub.Application.Features.Inventory.Transactions.Queries.GetInventoryTransactions;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý cấu hình kho và các dữ liệu mở đầu kỳ.
    /// </summary>
    [Tags("Kho hang - Cau hinh kho")]
    public class InventorySettingsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public InventorySettingsController(IMediator mediator, IMessageService messageService) : base(messageService)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy cấu hình kho hiện tại.
        /// </summary>
        /// <response code="200">Trả về cấu hình kho.</response>
        [HttpGet("/api/v{version:apiVersion}/inventory/settings")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<GetInventorySettingsResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetInventorySettings()
        {
            var result = await _mediator.Send(new GetInventorySettingsQuery());
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật cấu hình kho (cảnh báo hạn, tồn kho thấp, tự trừ tồn khi hoàn tất...).
        /// </summary>
        /// <param name="command">Giá trị cấu hình cần cập nhật.</param>
        /// <response code="200">Cập nhật thành công, trả về cấu hình mới.</response>
        [HttpPut("/api/v{version:apiVersion}/inventory/settings")]
        [HasPermission(Permissions.Inventory.Update)]
        [ProducesResponseType(
            typeof(Result<UpdateInventorySettingsResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> UpdateInventorySettings(
            [FromBody] UpdateInventorySettingsCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy danh sách tồn kho mở đầu kỳ (opening stock).
        /// </summary>
        /// <param name="pagination">Thông tin phân trang và tìm kiếm.</param>
        /// <response code="200">Danh sách tồn kho mở đầu kỳ.</response>
        [HttpGet("/api/v{version:apiVersion}/inventory/opening-stock")]
        [HttpGet("/api/v{version:apiVersion}/inventory/open-stocking")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetOpeningStockListResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetOpeningStockList(
            [FromQuery] PaginationParams pagination
        )
        {
            var result = await _mediator.Send(new GetOpeningStockListQuery(pagination));
            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy danh sách giao dịch tồn kho.
        /// </summary>
        /// <param name="pagination">Thông tin phân trang và tìm kiếm.</param>
        /// <response code="200">Danh sách giao dịch tồn kho.</response>
        [HttpGet("/api/v{version:apiVersion}/inventory/transactions")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetInventoryTransactionsResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetInventoryTransactions([FromQuery] PaginationParams pagination)
        {
            var result = await _mediator.Send(new GetInventoryTransactionsQuery(pagination));
            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }
            return HandleResult(result);
        }

        /// <summary>
        /// Khai báo tồn kho đầu kỳ.
        /// </summary>
        /// <param name="command">Danh sách nguyên liệu và số lượng mở đầu kỳ.</param>
        /// <response code="200">Ghi nhận tồn kho đầu kỳ thành công.</response>
        [HttpPost("/api/v{version:apiVersion}/inventory/opening-stock")]
        [HttpPost("/api/v{version:apiVersion}/inventory/open-stocking")]
        [HasPermission(Permissions.Inventory.Update)]
        [ProducesResponseType(typeof(Result<ImportOpeningStockResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ImportOpeningStock(
            [FromBody] ImportOpeningStockCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
