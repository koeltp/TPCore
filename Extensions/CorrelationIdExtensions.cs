using Taipi.Core.Middleware;

namespace Taipi.Core.Extensions;
/// <summary>
/// CorrelationId 中间件注册扩展
/// </summary>
public static class CorrelationIdExtensions
{

    /// <summary>
    /// 注册 CorrelationId 中间件，将请求链路标识注入 Serilog 日志上下文
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}