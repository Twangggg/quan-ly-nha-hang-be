using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Inventory.Ingredients.Commands.ActivateIngredient;
using FoodHub.Application.Features.Inventory.Ingredients.Commands.CreateIngredient;
using FoodHub.Application.Features.Inventory.Ingredients.Commands.DeactivateIngredient;
using FoodHub.Application.Features.Inventory.Ingredients.Commands.UpdateIngredient;
using FoodHub.Application.Features.Inventory.Ingredients.Queries.GenerateIngredientCode;
using FoodHub.Application.Features.Inventory.Ingredients.Queries.GetIngredientById;
using FoodHub.Application.Features.Inventory.Ingredients.Queries.GetIngredients;
using FoodHub.Application.Interfaces;
using FoodHub.WebAPI.Presentation.Attributes;
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
            command = command with { RouteId = id, IngredientId = id };
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
    }
}
