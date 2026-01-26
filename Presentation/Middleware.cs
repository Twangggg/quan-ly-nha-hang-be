using System.Net;
using FluentValidation;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;

namespace FoodHub.Presentation.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = (int)HttpStatusCode.InternalServerError;
        var message = "Internal server error";
        List<string>? errors = null;

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = (int)HttpStatusCode.BadRequest;
                message = "Validation failed";
                errors = validationException.Errors.Select(x => x.ErrorMessage).ToList();
                break;
            case BusinessException businessException:
                statusCode = (int)HttpStatusCode.BadRequest;
                message = businessException.Message;
                break;
            case NotFoundException notFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                message = notFoundException.Message;
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ErrorResponse(statusCode, message, errors);
        await context.Response.WriteAsJsonAsync(response);
    }
}
