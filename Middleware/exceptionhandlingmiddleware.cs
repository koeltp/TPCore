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
        var correlationId = GetOrCreateCorrelationId(context);
        result.CorrelationId = correlationId;

        // 3. 记录异常日志（根据配置和异常类型决定级别）
        if (_options.LogException)
        {
            var logLevel = GetLogLevel(exception);
            var shouldLogDetail = ShouldLogDetail(context);
            var errorMessage = shouldLogDetail
                ? exception.ToString()
                : $"异常类型: {exception.GetType().Name}, 消息: {exception.Message}";

            _logger.Log(logLevel, exception, "请求处理异常: {Message}", errorMessage);
        }

        // 4. 写入响应
        context.Response.StatusCode = httpStatusCode;
        context.Response.ContentType = "application/json";
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
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, StatusResponseResult.Error(_options.UnauthorizedCode, _options.UnauthorizedMessage)),
            ArgumentException argEx => (StatusCodes.Status400BadRequest, StatusResponseResult.Error(_options.BadRequestCode, argEx.Message)),
            KeyNotFoundException => (StatusCodes.Status404NotFound, StatusResponseResult.Error(_options.NotFoundCode, _options.NotFoundMessage)),
            // 3. 基类 AppException（注意：这里要排除已被上面处理的子类，但 switch 已按顺序，所以不会重复）
            AppException appEx => (StatusCodes.Status200OK, StatusResponseResult.Error(appEx.Code, appEx.Message)),
            // 4. 未知异常
            _ => (StatusCodes.Status500InternalServerError, StatusResponseResult.Error(_options.UnknownErrorCode, GetFinalErrorMessage(exception, context)))
        };
    }

    /// <summary>
    /// 获取最终的错误消息（生产环境/调试模式）
    /// </summary>
    private string GetFinalErrorMessage(Exception exception, HttpContext context)
    {
        var isDebugMode = _environment.IsDevelopment() ||
                          (_options.EnableDebugHeaderInProduction &&
                           context.Request.Headers.ContainsKey("X-Debug"));
        return isDebugMode
            ? _options.DetailedErrorMessageFactory(exception)
            : _options.UnknownErrorMessage;
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
    /// 判断是否记录详细异常堆栈（用于日志）
    /// </summary>
    private bool ShouldLogDetail(HttpContext context)
    {
        return _environment.IsDevelopment() ||
               (_options.EnableDebugHeaderInProduction && context.Request.Headers.ContainsKey("X-Debug"));
    }

    /// <summary>
    /// 获取或生成 CorrelationId，并存放到 HttpContext.Items 和日志上下文中
    /// </summary>
    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        const string correlationKey = "CorrelationId";
        if (context.Items.TryGetValue(correlationKey, out var value) && value is string cid && !string.IsNullOrEmpty(cid))
            return cid;

        // 尝试从请求头获取
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var headerCid) && !string.IsNullOrEmpty(headerCid))
        {
            context.Items[correlationKey] = headerCid.ToString();
            return headerCid.ToString();
        }

        // 生成新的
        var newCid = Guid.NewGuid().ToString("N");
        context.Items[correlationKey] = newCid;
        return newCid;
    }
}