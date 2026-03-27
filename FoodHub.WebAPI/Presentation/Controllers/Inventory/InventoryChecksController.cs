using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Inventory.InventoryChecks.Commands.CreateInventoryCheck;
using FoodHub.Application.Features.Inventory.InventoryChecks.Commands.ProcessInventoryCheck;
using FoodHub.Application.Features.Inventory.InventoryChecks.Queries.ExportInventoryCheck;
using FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryCheckById;
using FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryCheckCreateForm;
using FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryChecks;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Domain.Enums;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý phiếu kiểm kê tồn kho.
    /// </summary>
    [Tags("Kho hang - Kiem ke")]
    public class InventoryChecksController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IInventoryCheckExcelService _excelService;

        public InventoryChecksController(
            IMediator mediator,
            IMessageService messageService,
            IInventoryCheckExcelService excelService
        )
            : base(messageService)
        {
            _mediator = mediator;
            _excelService = excelService;
        }

        /// <summary>
        /// Lấy danh sách phiếu kiểm kê theo trạng thái, khoảng ngày và phân trang.
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/inventory/check")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetInventoryChecksResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetInventoryChecks(
            [FromQuery] PaginationParams pagination,
            [FromQuery] InventoryCheckStatus? status,
            [FromQuery] DateOnly? fromDate,
            [FromQuery] DateOnly? toDate
        )
        {
            var result = await _mediator.Send(
                new GetInventoryChecksQuery(pagination, status, fromDate, toDate)
            );

            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Lấy chi tiết một phiếu kiểm kê.
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/inventory/check/{id:guid}")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<GetInventoryCheckByIdResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetInventoryCheckById(Guid id)
        {
            var result = await _mediator.Send(new GetInventoryCheckByIdQuery(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy dữ liệu tạo phiếu kiểm kê với tồn theo sổ hiện tại của nguyên liệu.
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/inventory/check/create-form")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<IReadOnlyList<GetInventoryCheckCreateFormResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetInventoryCheckCreateForm()
        {
            var result = await _mediator.Send(new GetInventoryCheckCreateFormQuery());
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo mới phiếu kiểm kê ở trạng thái nháp.
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/inventory/check")]
        [HasPermission(Permissions.Inventory.Create)]
        [ProducesResponseType(
            typeof(Result<CreateInventoryCheckResponse>),
            StatusCodes.Status201Created
        )]
        public async Task<IActionResult> CreateInventoryCheck(
            [FromBody] CreateInventoryCheckCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleCreated(
                result,
                data =>
                    Url.Action(nameof(ProcessInventoryCheck), new { id = data.InventoryCheckId })
            );
        }

        /// <summary>
        /// Xử lý chênh lệch của phiếu kiểm kê.
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/inventory/check/{id:guid}/process")]
        [HasPermission(Permissions.Inventory.Update)]
        [ProducesResponseType(
            typeof(Result<ProcessInventoryCheckResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> ProcessInventoryCheck(Guid id)
        {
            var result = await _mediator.Send(new ProcessInventoryCheckCommand(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Xuất phiếu kiểm kê kho ra Excel.
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/inventory/check/{id:guid}/export")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportInventoryCheck(Guid id)
        {
            var result = await _mediator.Send(new ExportInventoryCheckQuery(id));
            if (!result.IsSuccess || result.Data == null)
            {
                return HandleResult(result);
            }

            var data = result.Data;
            var bytes = _excelService.ExportInventoryCheckToExcel(data);

            var fileName = $"Phieu_Kiem_Kho_{data.CheckDate:yyyyMMdd}_{data.InventoryCheckId}.xlsx";
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
    }
}
