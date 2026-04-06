using System.Net;
using FluentValidation;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.Security;
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

            if (!context.Response.HasStarted)
            {
                await HandleAuthStatusCodeAsync(context, messageService);
            }
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
                var validationErrors = validationException
                    .Errors.Select(x =>
                        messageService.HasKey(x.ErrorMessage)
                            ? messageService.GetMessage(x.ErrorMessage)
                            : x.ErrorMessage
                    )
                    .ToList();
                errors = validationErrors;
                message =
                    validationErrors.FirstOrDefault()
                    ?? messageService.GetMessage(MessageKeys.Common.ValidationFailed);
                break;
            case BusinessException businessException:
                statusCode = (int)HttpStatusCode.BadRequest;
                message = messageService.HasKey(businessException.Message)
                    ? messageService.GetMessage(businessException.Message)
                    : messageService.GetMessage(MessageKeys.Common.ValidationFailed);
                break;
            case NotFoundException notFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                message = messageService.HasKey(notFoundException.Message)
                    ? messageService.GetMessage(notFoundException.Message)
                    : messageService.GetMessage(MessageKeys.Common.NotFound);
                break;
            case ForbiddenException forbiddenException:
                statusCode = (int)HttpStatusCode.Forbidden;
                message = messageService.HasKey(forbiddenException.Message)
                    ? messageService.GetMessage(forbiddenException.Message)
                    : messageService.GetMessage(MessageKeys.Common.Forbidden);
                break;
            case UnauthorizedAccessException unauthorizedException:
                statusCode = (int)HttpStatusCode.Unauthorized;
                message = messageService.HasKey(unauthorizedException.Message)
                    ? messageService.GetMessage(unauthorizedException.Message)
                    : messageService.GetMessage(MessageKeys.Common.Unauthorized);
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

    private async Task HandleAuthStatusCodeAsync(
        HttpContext context,
        IMessageService messageService
    )
    {
        if (!ShouldWriteAuthErrorResponse(context.Response))
        {
            return;
        }

        var statusCode = context.Response.StatusCode;
        var message =
            statusCode == StatusCodes.Status401Unauthorized
                ? messageService.GetMessage(MessageKeys.Common.Unauthorized)
                : messageService.GetMessage(MessageKeys.Common.Forbidden);

        context.Response.ContentType = "application/json";

        var response = new ErrorResponse(statusCode, message);
        await context.Response.WriteAsJsonAsync(response);
    }

    private static bool ShouldWriteAuthErrorResponse(HttpResponse response)
    {
        if (
            response.StatusCode != StatusCodes.Status401Unauthorized
            && response.StatusCode != StatusCodes.Status403Forbidden
        )
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(response.ContentType))
        {
            return false;
        }

        return response.ContentLength is null or 0;
    }
}
