using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Taipi.Core.Exceptions;
using Taipi.Core.RQRS;
using Taipi.Core.Exceptions.Abstract;
using System.Collections.Concurrent;

namespace Taipi.Core.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly ExceptionHandlingOptions _options;

    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, Exception, HttpContext, (int, StatusResponseResult)>> _handlerCache = new();

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment,
        IOptions<ExceptionHandlingOptions> options)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
        _options = options.Value;
    }

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
        // 1. 确定 HTTP 状态码和响应结果
        var (httpStatusCode, result) = MapExceptionToResponse(exception, context);

        // 2. 附加 CorrelationId（确保存在）
        result.CorrelationId = context.Items["CorrelationId"] as string;

        // 3. 记录异常日志（根据配置和异常类型决定级别）
        if (_options.LogException)
        {
            var logLevel = GetLogLevel(exception);
            var errorMessage = IsDebugMode(context)
                ? exception.ToString()
                : $"异常类型: {exception.GetType().Name}, 消息: {exception.Message}";

            _logger.Log(logLevel, exception, "请求处理异常: {Message}", errorMessage);
        }

        // 4. 写入响应
        context.Response.StatusCode = httpStatusCode;
        context.Response.ContentType = "application/json";

        // ⭐ 如果是业务异常（200）或者任何错误响应，都禁用缓存，避免代理/CDN/浏览器缓存错误内容
        // 特别是对于 GET 请求返回 200 错误时，这条极其重要
        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        context.Response.Headers.Pragma = "no-cache"; // 兼容 HTTP/1.0
        context.Response.Headers.Expires = "0";       // 兼容 HTTP/1.0

        await context.Response.WriteAsync(JsonSerializer.Serialize(result, _jsonOptions));
    }


    private (int statusCode, StatusResponseResult result) MapExceptionToResponse(Exception exception, HttpContext context)
    {
        var exceptionType = exception.GetType();

        // 从缓存获取或创建执行委托
        var handlerFunc = _handlerCache.GetOrAdd(exceptionType, type =>
        {
            var handlerType = typeof(IExceptionHandler<>).MakeGenericType(type);
            var handleMethod = handlerType.GetMethod("Handle");
            if (handleMethod == null)
            {
                // 理论上不可能，IExceptionHandler<T> 强制实现了 Handle
                return null;
            }

            return new Func<IServiceProvider, Exception, HttpContext, (int, StatusResponseResult)>(
                (serviceProvider, ex, ctx) =>
                {
                    using (var scope = serviceProvider.CreateScope())
                    {
                        // 要求必须注册了对应的 Handler，否则抛出异常（明确提示配置错误）
                        var handler = scope.ServiceProvider.GetRequiredService(handlerType);
                        var result = handleMethod.Invoke(handler, [ex, ctx]);
                        return ((int, StatusResponseResult))result;
                    }
                });
        });

        // 如果缓存构建失败（几乎不可能），或者没有注册 Handler，GetRequiredService 会抛出异常
        return handlerFunc(context.RequestServices, exception, context);
    }

    /// <summary>
    /// 根据异常类型确定日志级别
    /// </summary>
    private static LogLevel GetLogLevel(Exception exception)
    {
        return exception switch
        {
            // 先匹配具体子类
            BadRequestException => LogLevel.Warning,
            ForbiddenException => LogLevel.Information,
            // 再匹配其他框架异常
            UnauthorizedAccessException => LogLevel.Information,
            ArgumentException => LogLevel.Warning,
            KeyNotFoundException => LogLevel.Information,
            // 最后匹配基类 AppException（捕获所有未单独处理的业务异常）
            AppException => LogLevel.Warning,
            // 未知异常
            _ => LogLevel.Error
        };
    }

    /// <summary>
    /// 判断是否为调试模式（开发环境或生产环境下的 X-Debug 头）
    /// </summary>
    /// <returns>是否为调试模式</returns>
    private bool IsDebugMode(HttpContext context)
    {
        return _environment.IsDevelopment() || (_options.EnableDebugHeaderInProduction && context.Request.Headers.ContainsKey("X-Debug"));
    }
}