using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Exceptions;

namespace QPS.API.Middleware;

/// <summary>Turns exceptions into the JSON error shape the client understands.</summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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
            if (context.Response.HasStarted) throw;

            var (status, message, errors) = ex switch
            {
                ValidationException v => (HttpStatusCode.BadRequest, v.Errors.FirstOrDefault()?.ErrorMessage ?? "Check the highlighted fields.",
                    v.Errors.GroupBy(e => ToCamel(e.PropertyName))
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray())),
                NotFoundException nf => (HttpStatusCode.NotFound, nf.Message, null),
                ForbiddenAccessException f => (HttpStatusCode.Forbidden, f.Message, null),
                UnauthorizedAccessException u => (HttpStatusCode.Unauthorized, u.Message, null),
                DbUpdateException => (HttpStatusCode.Conflict, "That change conflicts with existing data (it may already exist). Reload and try again.", null),
                BadHttpRequestException b => (HttpStatusCode.BadRequest, b.Message, null),
                _ => (HttpStatusCode.InternalServerError, "Something went wrong on the server. The error was logged.", (Dictionary<string, string[]>?)null)
            };

            if (status == HttpStatusCode.InternalServerError || ex is DbUpdateException)
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                status = (int)status,
                isSuccess = false,
                message,
                errors = errors?.Values.SelectMany(x => x).ToArray() ?? new[] { message },
                fieldErrors = errors
            }, Json));
        }
    }

    private static string ToCamel(string name)
    {
        if (string.IsNullOrEmpty(name)) return "general";
        var last = name.Split('.').Last();
        return char.ToLowerInvariant(last[0]) + last[1..];
    }
}
