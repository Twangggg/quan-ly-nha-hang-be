using System.IO;
using ClosedXML.Excel;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Inventory.ImportBalance.Commands.Import;
using FoodHub.Application.Features.Inventory.ImportBalance.Queries.ParseInventoryBalanceExcel;
using FoodHub.Application.Features.Inventory.Ingredients.Commands.ActivateIngredient;
using FoodHub.Application.Features.Inventory.Ingredients.Commands.CreateIngredient;
using FoodHub.Application.Features.Inventory.Ingredients.Commands.DeactivateIngredient;
using FoodHub.Application.Features.Inventory.Ingredients.Commands.UpdateIngredient;
using FoodHub.Application.Features.Inventory.Ingredients.Queries.GenerateIngredientCode;
using FoodHub.Application.Features.Inventory.Ingredients.Queries.GetIngredientById;
using FoodHub.Application.Features.Inventory.Ingredients.Queries.GetIngredients;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.WebAPI.Presentation.Attributes;
using FoodHub.WebAPI.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    /// <summary>
    /// Quản lý nguyên liệu, sinh mã, kích hoạt và vô hiệu hóa.
    /// </summary>
    [Tags("Kho hàng - Nguyên liệu (Ingredients)")]
    public class IngredientsController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMessageService _messageService;

        public IngredientsController(IMediator mediator, IMessageService messageService)
            : base(messageService)
        {
            _mediator = mediator;
            _messageService = messageService;
        }

        /// <summary>
        /// Lấy danh sách nguyên liệu với phân trang và lọc.
        /// </summary>
        [HttpGet]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(
            typeof(Result<PagedResult<GetIngredientsResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GetIngredients([FromQuery] PaginationParams pagination)
        {
            var result = await _mediator.Send(new GetIngredientsQuery(pagination));
            if (result.IsSuccess && result.Data != null)
            {
                Response.AddPaginationHeaders(result.Data);
            }
            return HandleResult(result);
        }

        /// <summary>
        /// Sinh mã nguyên liệu từ tên để FE có thể preview ngay khi nhập.
        /// </summary>
        [HttpGet("generate-code")]
        [HasPermission(Permissions.Inventory.Create)]
        [ProducesResponseType(
            typeof(Result<GenerateIngredientCodeResponse>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> GenerateIngredientCode([FromQuery] string name)
        {
            var result = await _mediator.Send(new GenerateIngredientCodeQuery(name));
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết nguyên liệu theo ID.
        /// </summary>
        [HttpGet("{id:guid}")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(typeof(Result<GetIngredientByIdResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetIngredientById(Guid id)
        {
            var result = await _mediator.Send(new GetIngredientByIdQuery(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo mới nguyên liệu.
        /// </summary>
        [HttpPost]
        [HasPermission(Permissions.Inventory.Create)]
        [ProducesResponseType(
            typeof(Result<CreateIngredientResponse>),
            StatusCodes.Status201Created
        )]
        public async Task<IActionResult> CreateIngredient(
            [FromBody] CreateIngredientCommand command
        )
        {
            var result = await _mediator.Send(command);
            return HandleCreated(
                result,
                data => Url.Action(nameof(GetIngredientById), new { id = data.IngredientId })
            );
        }

        /// <summary>
        /// Cập nhật nguyên liệu.
        /// </summary>
        [HttpPut("{id:guid}")]
        [HasPermission(Permissions.Inventory.Update)]
        [ProducesResponseType(typeof(Result<UpdateIngredientResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateIngredient(
            Guid id,
            [FromBody] UpdateIngredientCommand command
        )
        {
            command = command with { IngredientId = id };
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Vô hiệu hóa nguyên liệu.
        /// </summary>
        [HttpPatch("{id:guid}/deactivate")]
        [HasPermission(Permissions.Inventory.Deactivate)]
        [ProducesResponseType(typeof(Result<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeactivateIngredient(Guid id)
        {
            var result = await _mediator.Send(new DeactivateIngredientCommand(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Kích hoạt lại nguyên liệu.
        /// </summary>
        [HttpPatch("{id:guid}/activate")]
        [HasPermission(Permissions.Inventory.Update)]
        [ProducesResponseType(typeof(Result<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ActivateIngredient(Guid id)
        {
            var result = await _mediator.Send(new ActivateIngredientCommand(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Nhập số dư tồn kho từ file Excel.
        /// </summary>
        [HttpPost("import-balance")]
        [HasPermission(Permissions.Inventory.Import)]
        [ProducesResponseType(
            typeof(Result<ImportInventoryBalanceResponse>),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(
            typeof(Result<ImportInventoryBalanceResponse>),
            StatusCodes.Status400BadRequest
        )]
        public async Task<IActionResult> ImportInventoryBalance(
            IFormFile file,
            [FromQuery] bool confirmOverwrite = false
        )
        {
            var result = await _mediator.Send(
                new ImportInventoryBalanceCommand(file, confirmOverwrite)
            );
            return HandleResult(result);
        }

        /// <summary>
        /// Phân tích file Excel để xem trước (Preview) dữ liệu trước khi lưu.
        /// </summary>
        [HttpPost("parse-balance")]
        [HasPermission(Permissions.Inventory.Import)]
        [ProducesResponseType(
            typeof(Result<List<ParsedInventoryBalanceResponse>>),
            StatusCodes.Status200OK
        )]
        public async Task<IActionResult> ParseInventoryBalance(IFormFile file)
        {
            var result = await _mediator.Send(new ParseInventoryBalanceExcelQuery(file));
            return HandleResult(result);
        }

        /// <summary>
        /// Tải file mẫu nhập tồn kho Excel.
        /// </summary>
        [HttpGet("import-balance/template")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public IActionResult DownloadImportBalanceTemplate()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Mau_Nhap_Ton_Kho");

            sheet.Cell(1, 1).Value = "Mã nguyên liệu";
            sheet.Cell(1, 2).Value = "Số lượng";
            sheet.Cell(1, 3).Value = "Giá nhập";
            sheet.Cell(1, 4).Value = "Đơn vị";

            sheet.Range(1, 1, 1, 4).Style.Font.Bold = true;
            sheet.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.LightBlue;

            sheet.Cell(2, 1).Value = "NL001";
            sheet.Cell(2, 2).Value = 100;
            sheet.Cell(2, 3).Value = 50000;
            sheet.Cell(2, 4).Value = "kg";

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var bytes = stream.ToArray();

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Mau_Nhap_Ton_Kho.xlsx"
            );
        }
    }
}
