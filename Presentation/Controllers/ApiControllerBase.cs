using FoodHub.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result == null) return BadRequest();

            if (result.IsSuccess)
            {
                if (result.Data == null) return NoContent();

                if (result.HasWarning)
                {
                    return Ok(new
                    {
                        data = result.Data,
                        warning = result.Warning
                    });
                }

                return Ok(result.Data);
            }

            return result.ErrorType switch
            {
                ResultErrorType.BadRequest => BadRequest(new { message = result.Error }),
                ResultErrorType.NotFound => NotFound(new { message = result.Error }),
                ResultErrorType.Unauthorized => Unauthorized(new { message = result.Error }),
                ResultErrorType.Forbidden => Forbid(),
                ResultErrorType.Conflict => Conflict(new { message = result.Error }),
                _ => BadRequest(new { message = result.Error })
            };
        }
    }
}
