using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Inventory.Recipes.Commands.UpsertRecipe;
using FoodHub.Application.Features.Inventory.Recipes.Queries.GetRecipe;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Presentation.Controllers;
using FoodHub.WebAPI.Presentation.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    [ApiController]
    [Authorize]
    /// <summary>
    /// Quản lý định lượng nguyên liệu (recipe) cho món ăn.
    /// </summary>
    [Tags("Kho hàng - Định lượng (Recipe)")]
    public class RecipesController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMessageService _messageService;

        public RecipesController(IMediator mediator, IMessageService messageService)
        {
            _mediator = mediator;
            _messageService = messageService;
        }

        /// <summary>
        /// Lấy recipe của một món ăn.
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/inventory/recipes/{menuItemId:guid}")]
        [HasPermission(Permissions.Inventory.View)]
        [ProducesResponseType(typeof(Result<List<GetRecipeItemResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get(Guid menuItemId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetRecipeQuery(menuItemId), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Tạo/cập nhật recipe cho một món (thay thế toàn bộ danh sách nguyên liệu của món).
        /// </summary>
        [HttpPut("/api/v{version:apiVersion}/inventory/recipes/{menuItemId:guid}")]
        [HasPermission(Permissions.Inventory.Update)]
        [ProducesResponseType(typeof(Result<Unit>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Upsert(
            Guid menuItemId,
            [FromBody] UpsertRecipeRequest request,
            CancellationToken cancellationToken
        )
        {
            var command = new UpsertRecipeCommand(
                menuItemId,
                request.Items,
                request.Instructions,
                request.PrepTimeMinutes
            );
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }
    }
}
