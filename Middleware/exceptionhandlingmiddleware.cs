using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Taipi.Core.Exceptions;
using Taipi.Core.RQRS;

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

    /// <summary>
    /// 根据异常类型映射到 HTTP 状态码和业务响应
    /// </summary>
    private (int statusCode, StatusResponseResult result) MapExceptionToResponse(Exception exception, HttpContext context)
    {
        return exception switch
        {
            // 1. 具体子类优先
            BadRequestException badEx => (StatusCodes.Status400BadRequest, StatusResponseResult.Error(badEx.Code, badEx.Message)),
            ForbiddenException forbidEx => (StatusCodes.Status403Forbidden, StatusResponseResult.Error(forbidEx.Code, forbidEx.Message)),
            // 2. 框架异常
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, StatusResponseResult.Error(_options.UnauthorizedCode, GetFinalErrorMessage(exception, context, _options.UnauthorizedMessage))),
            ArgumentException => (StatusCodes.Status400BadRequest, StatusResponseResult.Error(_options.BadRequestCode, GetFinalErrorMessage(exception, context, _options.BadRequestMessage))),
            KeyNotFoundException => (StatusCodes.Status404NotFound, StatusResponseResult.Error(_options.NotFoundCode, GetFinalErrorMessage(exception, context, _options.NotFoundMessage))),
            // 3. 基类 AppException（注意：这里要排除已被上面处理的子类，但 switch 已按顺序，所以不会重复）
            AppException appEx => (StatusCodes.Status200OK, StatusResponseResult.Error(appEx.Code, appEx.Message)),
            // 4. 未知异常
            _ => (StatusCodes.Status500InternalServerError, StatusResponseResult.Error(_options.UnknownErrorCode, GetFinalErrorMessage(exception, context, _options.UnknownErrorMessage)))
        };
    }

    /// <summary>
    /// 获取最终的错误消息（生产环境/调试模式）
    /// </summary>
    private string GetFinalErrorMessage(Exception exception, HttpContext context, string defaultMessage)
    {
        return IsDebugMode(context)
            ? _options.DetailedErrorMessageFactory(exception)
            : defaultMessage;
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