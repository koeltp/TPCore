using Serilog.Context;

namespace Taipi.Core.Middleware;

/// <summary>
/// CorrelationId 中间件：从请求头获取或生成唯一 CorrelationId，
/// 注入 Serilog LogContext 使所有日志都携带此标识，便于链路追踪
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N")[..16];

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers.Append("X-Correlation-Id", correlationId);

        // 将 CorrelationId 注入 Serilog 上下文，后续所有日志自动携带
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
