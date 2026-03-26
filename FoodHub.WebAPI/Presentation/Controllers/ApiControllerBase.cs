using Asp.Versioning;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FoodHub.Presentation.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public abstract class ApiControllerBase : ControllerBase
    {
        private IMessageService? _messageService;

        protected IMessageService MessageService =>
            _messageService ??= HttpContext.RequestServices.GetRequiredService<IMessageService>();

        protected ApiControllerBase() { }

        protected ApiControllerBase(IMessageService messageService)
        {
            _messageService = messageService;
        }

        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result == null)
            {
                var errorResponse = new ErrorResponse(
                    StatusCodes.Status400BadRequest,
                    MessageService.GetMessage(MessageKeys.Common.ValidationFailed)
                );
                return BadRequest(errorResponse);
            }

            if (result.IsSuccess)
            {
                if (result.Data == null)
                    return NoContent();

                if (result.HasWarning)
                {
                    return Ok(new { data = result.Data, warning = result.Warning });
                }

                return Ok(new { data = result.Data });
            }

            var statusCode = result.ErrorType switch
            {
                ResultErrorType.NotFound => 404,
                ResultErrorType.Unauthorized => 401,
                ResultErrorType.Forbidden => 403,
                ResultErrorType.Conflict => 409,
                _ => 400,
            };

            var response = new ErrorResponse(
                statusCode,
                result.Error ?? MessageService.GetMessage(MessageKeys.Common.InternalServerError)
            );
            return StatusCode(statusCode, response);
        }

        protected IActionResult HandleCreated<T>(Result<T> result, Func<T, string?> locationFunc)
        {
            if (result == null)
            {
                var errorResponse = new ErrorResponse(
                    StatusCodes.Status400BadRequest,
                    MessageService.GetMessage(MessageKeys.Common.ValidationFailed)
                );
                return BadRequest(errorResponse);
            }

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
            if (result == null)
            {
                var errorResponse = new ErrorResponse(
                    StatusCodes.Status400BadRequest,
                    MessageService.GetMessage(MessageKeys.Common.ValidationFailed)
                );
                return BadRequest(errorResponse);
            }

            if (!result.IsSuccess)
            {
                return HandleResult(result);
            }

            if (result.Data == null)
            {
                var errorResponse = new ErrorResponse(
                    StatusCodes.Status404NotFound,
                    MessageService.GetMessage(MessageKeys.Common.NotFound)
                );
                return NotFound(errorResponse);
            }

            var fileName = fileNameFunc?.Invoke(result.Data);
            var content = contentFunc(result.Data);

            return string.IsNullOrWhiteSpace(fileName)
                ? File(content, contentType)
                : File(content, contentType, fileName);
        }
    }
}
