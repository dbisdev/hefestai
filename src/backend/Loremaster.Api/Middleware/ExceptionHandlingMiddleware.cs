using System.Net;
using System.Text.Json;
using Loremaster.Domain.Exceptions;

namespace Loremaster.Api.Middleware;

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
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case ValidationException validationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = validationException.Message;
                response.Errors = validationException.Errors;
                break;

            case NotFoundException notFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = notFoundException.Message;
                break;

            case ForbiddenAccessException forbiddenException:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.Message = forbiddenException.Message;
                break;

            case DomainException domainException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = domainException.Message;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "An error occurred while processing your request.";
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public IDictionary<string, string[]>? Errors { get; set; }
}
