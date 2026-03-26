using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Inventory.InventoryChecks.Commands.CreateInventoryCheck;
using FoodHub.Application.Features.Inventory.InventoryChecks.Commands.ProcessInventoryCheck;
using FoodHub.Application.Features.Inventory.InventoryChecks.Queries.ExportInventoryCheck;
using FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryCheckById;
using FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryCheckCreateForm;
using FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryChecks;
using FoodHub.Domain.Enums;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using System.IO;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý phiếu kiểm kê tồn kho.
    /// </summary>
    [Tags("Kho hang - Kiem ke")]
    public class InventoryChecksController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public InventoryChecksController(IMediator mediator, IMessageService messageService) : base(messageService)
        {
            _mediator = mediator;
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
                data => Url.Action(nameof(ProcessInventoryCheck), new { id = data.InventoryCheckId })
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

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Phieu_Kiem_Kho");

            sheet.Cell(1, 1).Value = "PHIẾU KIỂM KHO";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;
            sheet.Range(1, 1, 1, 7).Merge();

            sheet.Cell(2, 1).Value = $"Mã phiếu: {data.InventoryCheckId}";
            sheet.Cell(3, 1).Value = $"Ngày kiểm kê: {data.CheckDate:dd/MM/yyyy}";
            sheet.Cell(4, 1).Value = $"Trạng thái: {data.Status}";
            sheet.Cell(5, 1).Value = $"Ngày tạo: {data.CreatedAt:dd/MM/yyyy HH:mm}";
            sheet.Cell(6, 1).Value = $"Số mặt hàng: {data.TotalItems}";

            var headerRow = 8;
            sheet.Cell(headerRow, 1).Value = "STT";
            sheet.Cell(headerRow, 2).Value = "Mã nguyên liệu";
            sheet.Cell(headerRow, 3).Value = "Tên nguyên liệu";
            sheet.Cell(headerRow, 4).Value = "Đơn vị";
            sheet.Cell(headerRow, 5).Value = "Tồn theo sổ";
            sheet.Cell(headerRow, 6).Value = "Tồn thực tế";
            sheet.Cell(headerRow, 7).Value = "Chênh lệch";
            sheet.Cell(headerRow, 8).Value = "Giá trị sổ";
            sheet.Cell(headerRow, 9).Value = "Giá trị thực tế";
            sheet.Cell(headerRow, 10).Value = "Chênh lệch giá trị";
            sheet.Cell(headerRow, 11).Value = "Ghi chú";

            sheet.Range(headerRow, 1, headerRow, 11).Style.Font.Bold = true;
            sheet.Range(headerRow, 1, headerRow, 11).Style.Fill.BackgroundColor = XLColor.LightBlue;

            var currentRow = headerRow + 1;
            for (int i = 0; i < data.Items.Count; i++)
            {
                var item = data.Items[i];
                sheet.Cell(currentRow, 1).Value = i + 1;
                sheet.Cell(currentRow, 2).Value = item.IngredientCode;
                sheet.Cell(currentRow, 3).Value = item.IngredientName;
                sheet.Cell(currentRow, 4).Value = item.Unit;
                sheet.Cell(currentRow, 5).Value = item.BookQuantity;
                sheet.Cell(currentRow, 6).Value = item.PhysicalQuantity;
                sheet.Cell(currentRow, 7).Value = item.DifferenceQuantity;
                sheet.Cell(currentRow, 7).Style.Font.FontColor = item.DifferenceQuantity != 0 ? XLColor.Red : XLColor.Black;
                sheet.Cell(currentRow, 8).Value = item.BookValue;
                sheet.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0";
                sheet.Cell(currentRow, 9).Value = item.PhysicalValue;
                sheet.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0";
                sheet.Cell(currentRow, 10).Value = item.DifferenceValue;
                sheet.Cell(currentRow, 10).Style.NumberFormat.Format = "#,##0";
                sheet.Cell(currentRow, 10).Style.Font.FontColor = item.DifferenceValue != 0 ? XLColor.Red : XLColor.Black;
                sheet.Cell(currentRow, 11).Value = item.Reason;

                currentRow++;
            }

            currentRow++;
            sheet.Cell(currentRow, 1).Value = "Tổng cộng:";
            sheet.Cell(currentRow, 1).Style.Font.Bold = true;
            sheet.Cell(currentRow, 5).Value = data.Items.Sum(x => x.BookQuantity);
            sheet.Cell(currentRow, 6).Value = data.Items.Sum(x => x.PhysicalQuantity);
            sheet.Cell(currentRow, 7).Value = data.TotalDifferenceValue;
            sheet.Cell(currentRow, 7).Style.Font.Bold = true;
            sheet.Cell(currentRow, 7).Style.Font.FontColor = data.TotalDifferenceValue != 0 ? XLColor.Red : XLColor.Black;
            sheet.Cell(currentRow, 8).Value = data.TotalBookValue;
            sheet.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0";
            sheet.Cell(currentRow, 8).Style.Font.Bold = true;
            sheet.Cell(currentRow, 9).Value = data.TotalPhysicalValue;
            sheet.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0";
            sheet.Cell(currentRow, 9).Style.Font.Bold = true;
            sheet.Cell(currentRow, 10).Value = data.TotalDifferenceValue;
            sheet.Cell(currentRow, 10).Style.NumberFormat.Format = "#,##0";
            sheet.Cell(currentRow, 10).Style.Font.Bold = true;
            sheet.Cell(currentRow, 10).Style.Font.FontColor = data.TotalDifferenceValue != 0 ? XLColor.Red : XLColor.Black;

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var bytes = stream.ToArray();

            var fileName = $"Phieu_Kiem_Kho_{data.CheckDate:yyyyMMdd}_{data.InventoryCheckId}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
