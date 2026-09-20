using System.Net;
using System.Text.Json;
using AgilentQuiz.Application.Common;

namespace AgilentQuiz.Api.Middleware;

/// <summary>
/// 全局异常处理中间件：捕获业务异常与未处理异常，统一返回 ApiResponse
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

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
        catch (BusinessException bex)
        {
            _logger.LogWarning("Business exception: {Code} - {Message}", bex.Code, bex.Message);
            await WriteErrorResponseAsync(context, HttpStatusCode.BadRequest, bex.Code, bex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorResponseAsync(context, HttpStatusCode.InternalServerError, "9999", "服务器内部错误，请稍后重试");
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, HttpStatusCode statusCode, string code, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(code, message);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await context.Response.WriteAsync(json);
    }
}
