using System.Net;
using FluentValidation;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using Microsoft.Extensions.Hosting;

namespace FoodHub.Presentation.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment env
    )
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task Invoke(HttpContext context, IMessageService messageService)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, messageService);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        IMessageService messageService
    )
    {
        var statusCode = (int)HttpStatusCode.InternalServerError;
        var message = messageService.GetMessage(MessageKeys.Common.InternalServerError);
        List<string>? errors = null;

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = (int)HttpStatusCode.BadRequest;
                errors = validationException.Errors.Select(x => x.ErrorMessage).ToList();
                message =
                    errors.FirstOrDefault()
                    ?? messageService.GetMessage(MessageKeys.Common.ValidationFailed);
                break;
            case BusinessException businessException:
                statusCode = (int)HttpStatusCode.BadRequest;
                message = businessException.Message;
                break;
            case NotFoundException notFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                message = notFoundException.Message;
                break;
            default:
                if (_env.IsDevelopment())
                {
                    message = exception.Message;
                    errors = new List<string> { exception.ToString() };
                }
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ErrorResponse(statusCode, message, errors);
        await context.Response.WriteAsJsonAsync(response);
    }
}
