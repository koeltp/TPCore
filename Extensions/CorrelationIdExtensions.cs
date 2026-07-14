using Taipi.Core.Middleware;

namespace Taipi.Core.Extensions;

/// <summary>
/// CorrelationId 中间件注册扩展
/// </summary>
public static class CorrelationIdExtensions
{
    /// <summary>
    /// 注册 CorrelationId 中间件配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">可选配置回调，覆盖默认的头名称、ID格式等</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddTaiPiCorrelationId(
        this IServiceCollection services,
        Action<CorrelationIdOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);

        return services;
    }

    /// <summary>
    /// 注册 CorrelationId 中间件，将请求链路标识注入 Serilog 日志上下文
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
