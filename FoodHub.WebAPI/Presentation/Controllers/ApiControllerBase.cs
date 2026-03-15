using Asp.Versioning;
using FoodHub.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Presentation.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
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

                return Ok(new { data = result.Data });
            }

            var statusCode = result.ErrorType switch
            {
                ResultErrorType.NotFound => 404,
                ResultErrorType.Unauthorized => 401,
                ResultErrorType.Forbidden => 403,
                ResultErrorType.Conflict => 409,
                _ => 400
            };

            var response = new ErrorResponse(statusCode, result.Error ?? "An error occurred");
            return StatusCode(statusCode, response);
        }

        protected IActionResult HandleCreated<T>(Result<T> result, Func<T, string?> locationFunc)
        {
            if (result == null) return BadRequest();

            if (!result.IsSuccess)
            {
                return HandleResult(result);
            }

            if (result.Data == null)
            {
                return StatusCode(StatusCodes.Status201Created);
            }

            var location = locationFunc(result.Data);
            if (string.IsNullOrEmpty(location))
            {
                return Created(string.Empty, new { data = result.Data });
            }

            return Created(location, new { data = result.Data });
        }

        protected IActionResult HandleFileResult<T>(
            Result<T> result,
            Func<T, byte[]> contentFunc,
            string contentType,
            Func<T, string?>? fileNameFunc = null
        )
        {
            if (result == null) return BadRequest();

            if (!result.IsSuccess)
            {
                return HandleResult(result);
            }

            if (result.Data == null)
            {
                return NoContent();
            }

            var fileName = fileNameFunc?.Invoke(result.Data);
            var content = contentFunc(result.Data);

            return string.IsNullOrWhiteSpace(fileName)
                ? File(content, contentType)
                : File(content, contentType, fileName);
        }
    }
}
