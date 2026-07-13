using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Taipi.Core.Exceptions;
using Taipi.Core.RQRS;
using Taipi.Core.Exceptions.Abstract;

namespace Taipi.Core.Middleware;

/// <summary>
/// 全局异常处理中间件：捕获管道中的异常，沿继承链查找 Handler 处理，
/// 编译委托缓存以优化性能，未匹配的异常回退到 UnknownExceptionHandler
/// </summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// 创建异常处理中间件实例
    /// </summary>
    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 处理 HTTP 请求，捕获管道中的异常并委托给 Handler 处理
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(ex, "响应已开始发送，无法写入异常响应：{Message}", ex.Message);
                return;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // 1. 确定 HTTP 状态码、响应结果和日志级别
        var (httpStatusCode, result, logLevel) = MapExceptionToResponse(exception, context);

        // 2. 附加 CorrelationId
        result.CorrelationId = context.Items["CorrelationId"] as string;

        // 3. 将日志级别存入 HttpContext.Items，供下游中间件（如请求日志）使用
        context.Items["ExceptionLogLevel"] = logLevel;

        // 4. 记录异常日志（日志级别由 Handler 决定）
        _logger.Log(logLevel, exception, "请求处理异常: {ExceptionType}, {Message}", exception.GetType().Name, exception.Message);

        // 5. 写入响应
        context.Response.StatusCode = httpStatusCode;
        context.Response.ContentType = "application/json";

        // 禁用缓存，避免代理/CDN/浏览器缓存错误内容（特别是 GET 请求返回 200 错误时）
        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";

        await context.Response.WriteAsync(JsonSerializer.Serialize(result, _jsonOptions));
    }

    /// <summary>
    /// 根据异常类型映射到 HTTP 状态码、响应结果和日志级别。
    /// 使用 <see cref="ExceptionHandlerDelegateCache"/> 共享缓存和编译委托。
    /// </summary>
    private (int statusCode, StatusResponseResult result, LogLevel logLevel) MapExceptionToResponse(Exception exception, HttpContext context)
    {
        var handlerInterfaceType = ExceptionHandlerDelegateCache.ResolveHandlerType(exception.GetType(), context.RequestServices);
        var (handleFunc, getLogLevelFunc) = ExceptionHandlerDelegateCache.GetOrCompileDelegates(handlerInterfaceType);

        using var scope = context.RequestServices.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService(handlerInterfaceType);
        var (statusCode, result) = handleFunc(handler, exception, context);
        var logLevel = getLogLevelFunc(handler, exception);

        return (statusCode, result, logLevel);
    }
}
