using Taipi.Core.Middleware;

namespace Taipi.Core.Extensions;

/// <summary>
/// 请求日志中间件注册扩展
/// </summary>
public static class RequestLoggingExtensions
{
    /// <summary>
    /// 添加请求日志中间件配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">可选配置回调，覆盖默认的过滤规则、脱敏字段等</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddTaiPiRequestLogging(this IServiceCollection services, Action<RequestLoggingOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);
        return services;
    }

    /// <summary>
    /// 启用请求日志中间件
    /// </summary>
    public static IApplicationBuilder UseTaiPiRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}