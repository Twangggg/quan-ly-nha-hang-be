using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Inventory.Costing.Commands.RecalculateCogs;
using FoodHub.Application.Features.Inventory.Reports.Queries.GetInventoryLedger;
using FoodHub.Application.Features.Inventory.Reports.Queries.GetInventoryReport;
using FoodHub.Domain.Enums;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Báo cáo và sổ cái tồn kho.
    /// </summary>
    [Tags("Kho hang - Bao cao")]
    public class InventoryReportsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public InventoryReportsController(IMediator mediator, IMessageService messageService) : base(messageService)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy báo cáo tồn kho theo khoảng thời gian.
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/inventory/report")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetInventoryReportResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetInventoryReport(
            [FromQuery] PaginationParams pagination,
            [FromQuery] DateOnly fromDate,
            [FromQuery] DateOnly toDate,
            [FromQuery] Guid? ingredientId
        )
        {
            var result = await _mediator.Send(
                new GetInventoryReportQuery(pagination, fromDate, toDate, ingredientId)
            );

            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Lấy sổ cái biến động tồn kho.
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/inventory/ledger")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetInventoryLedgerResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetInventoryLedger(
            [FromQuery] Guid? ingredientId,
            [FromQuery] DateOnly fromDate,
            [FromQuery] DateOnly toDate,
            [FromQuery] InventoryTransactionType? transactionType
        )
        {
            var result = await _mediator.Send(
                new GetInventoryLedgerQuery(ingredientId, fromDate, toDate, transactionType)
            );
            return HandleResult(result);
        }

        /// <summary>
        /// Tinh lai gia von xuat kho trong mot ky toi da 31 ngay.
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/inventory/cogs/recalculate")]
        [HasPermission(Permissions.Inventory.Update)]
        [ProducesResponseType(
            typeof(Result<RecalculateCogsResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> RecalculateCogs(
            [FromBody] RecalculateCogsCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
