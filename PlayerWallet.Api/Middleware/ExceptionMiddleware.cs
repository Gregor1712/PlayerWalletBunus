using System.Net;
using System.Text.Json;
using PlayerWallet.Application.Exceptions;
using PlayerWallet.Application.Models;

namespace PlayerWallet.Api.Middleware;

public class ExceptionMiddleware(IHostEnvironment env, RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (WalletAlreadyExistsException e)
        {
            logger.LogWarning(e, "Conflict on {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, e, HttpStatusCode.Conflict);
        }
        catch (WalletNotFoundException e)
        {
            logger.LogWarning(e, "Not found on {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, e, HttpStatusCode.NotFound);
        }
        catch (ArgumentOutOfRangeException e)
        {
            logger.LogWarning(e, "Bad request on {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, e, HttpStatusCode.BadRequest);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, e, HttpStatusCode.InternalServerError);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception e, HttpStatusCode statusCode)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = env.IsDevelopment()
            ? new ApiErrorResponse(context.Response.StatusCode, e.Message, e.StackTrace)
            : new ApiErrorResponse(context.Response.StatusCode, e.Message, "Internal Server error");

        logger.LogWarning("Returning ApiErrorResponse: StatusCode={StatusCode}, Message={Message}, Details={Details}",
            response.StatusCode, response.Message, response.Details);

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        return context.Response.WriteAsync(json);
    }
}
