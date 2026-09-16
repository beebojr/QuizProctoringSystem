using System.Net;
using System.Text.Json;
using FluentValidation;

namespace QPS.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "Validation failed",
                validationEx.Errors.ToDictionary(
                    e => e.PropertyName,
                    e => new[] { e.ErrorMessage })),
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                "Resource not found",
                (Dictionary<string, string[]>?)null),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                (Dictionary<string, string[]>?)null),
            _ => (
                HttpStatusCode.InternalServerError,
                "An error occurred",
                (Dictionary<string, string[]>?)null)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            status = (int)statusCode,
            message,
            errors
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}