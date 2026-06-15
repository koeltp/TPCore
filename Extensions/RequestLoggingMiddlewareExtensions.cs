using Taipi.Core.Middleware;

namespace Taipi.Core.Extensions;

public static class RequestLoggingExtensions
{
    /// <summary>
    /// 添加请求日志中间件配置
    /// </summary>
    public static IServiceCollection AddRequestLogging(this IServiceCollection services, Action<RequestLoggingOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);
        else
            services.Configure<RequestLoggingOptions>(_ => { });
        return services;
    }

    /// <summary>
    /// 启用请求日志中间件
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}