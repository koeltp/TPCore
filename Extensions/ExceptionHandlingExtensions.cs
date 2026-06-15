using Taipi.Core.Middleware;

namespace Taipi.Core.Extensions;

/// <summary>
/// 异常处理中间件服务扩展方法
/// </summary>
public static class ExceptionHandlingExtensions
{
    public static IServiceCollection AddTaiPiExceptionHandling(
        this IServiceCollection services,
        Action<ExceptionHandlingOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);
        else
            services.Configure<ExceptionHandlingOptions>(_ => { });
        return services;
    }

    public static IApplicationBuilder UseTaiPiExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}