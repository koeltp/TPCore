using Serilog.Context;

namespace Taipi.Core.Middleware;

/// <summary>
/// CorrelationId 中间件：从请求头获取或生成唯一 CorrelationId，
/// 注入 Serilog LogContext 使所有日志都携带此标识，便于链路追踪
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// 创建 CorrelationId 中间件实例
    /// </summary>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// 处理 HTTP 请求，获取或生成 CorrelationId 并注入日志上下文
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrGenerateCorrelationId(context);

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers.Append("X-Correlation-Id", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    /// <summary>
    /// 从请求头获取 CorrelationId（校验格式），不存在或格式非法时生成新 ID
    /// </summary>
    private static string GetOrGenerateCorrelationId(HttpContext context)
    {
        var headerValue = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(headerValue) && IsValidCorrelationId(headerValue))
            return headerValue;

        return Guid.NewGuid().ToString("N")[..16];
    }

    /// <summary>
    /// 校验 CorrelationId 格式：仅允许字母、数字和连字符，长度 1-64，防止注入非法字符
    /// </summary>
    private static bool IsValidCorrelationId(string value)
    {
        return value.Length <= 64 && value.All(c => char.IsLetterOrDigit(c) || c == '-');
    }
}
