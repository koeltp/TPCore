using System.Text.Json;
using System.Text.Json.Serialization;
using Taipi.Core.Exceptions;
using Taipi.Core.RQRS;

namespace Taipi.Core.Middleware;

/// <summary>
/// 全局异常处理中间件：
/// - AppException（业务异常）→ HTTP 200 + 业务错误码
/// - 框架异常 → HTTP 4xx/5xx + 系统错误码
/// </summary>
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
        ExceptionHandlingOptions options)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
        _options = options;
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

            _logger.LogError(ex, "未处理的异常：{Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (httpStatusCode, result) = exception switch
        {
            // 业务异常：HTTP 200 + 业务错误码，前端在 .then 中判断 code
            AppException appEx => (StatusCodes.Status200OK,
                StatusResponseResult.Error(appEx.Code, appEx.Message)),

            // 框架异常：HTTP 4xx/5xx + 系统错误码，前端在 axios 拦截器中处理
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized,
                StatusResponseResult.Error(_options.UnauthorizedCode, _options.UnauthorizedMessage)),
            ArgumentException => (StatusCodes.Status400BadRequest,
                StatusResponseResult.Error(_options.BadRequestCode, exception.Message)),
            KeyNotFoundException => (StatusCodes.Status404NotFound,
                StatusResponseResult.Error(_options.NotFoundCode, _options.NotFoundMessage)),

            // 未知异常：HTTP 500
            _ => (StatusCodes.Status500InternalServerError,
                StatusResponseResult.Error(_options.UnknownErrorCode,
                    _environment.IsProduction()
                        ? _options.UnknownErrorMessage
                        : _options.DetailedErrorMessageFactory(exception)))
        };

        // 附加 correlationId 便于前端/日志关联
        result.CorrelationId = context.Items["CorrelationId"]?.ToString();

        context.Response.StatusCode = httpStatusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(result, _jsonOptions));
    }
}
