using System.Net;
using System.Text.Json;
using Studio.Api.Application.Common;
using Studio.Api.Application.Common.Exceptions;

namespace Studio.Api.Infrastructure.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        HttpStatusCode statusCode;
        string message;
        List<string>? errors = null;

        switch (exception)
        {
            case AppException appEx:
                statusCode = appEx.StatusCode;
                message = appEx.Message;
                errors = appEx.Errors;
                _logger.LogWarning("Application exception ({StatusCode}): {Message}", statusCode, message);
                break;

            case KeyNotFoundException knf:
                statusCode = HttpStatusCode.NotFound;
                message = knf.Message;
                _logger.LogWarning("KeyNotFound exception: {Message}", message);
                break;

            case InvalidOperationException invOp:
                if (invOp.Message.Contains("currently processing") || invOp.Message.Contains("already running") || invOp.Message.Contains("Duplicate execution"))
                {
                    statusCode = HttpStatusCode.Conflict;
                }
                else
                {
                    statusCode = HttpStatusCode.BadRequest;
                }
                message = invOp.Message;
                _logger.LogWarning("InvalidOperation exception ({StatusCode}): {Message}", statusCode, message);
                break;

            default:
                statusCode = HttpStatusCode.InternalServerError;
                message = "An unexpected server error occurred.";
                _logger.LogError(exception, "Unhandled server error: {Message}", exception.Message);
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(message, errors, (int)statusCode);
        var json = JsonSerializer.Serialize(response, JsonOptions);

        await context.Response.WriteAsync(json);
    }
}
