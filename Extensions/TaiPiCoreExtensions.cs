using Taipi.Core.Middleware;

namespace Taipi.Core;

/// <summary>
/// TaiPi.Core 服务注册扩展方法
/// </summary>
public static class TaiPiCoreExtensions
{
    /// <summary>
    /// 注册全局异常处理中间件服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">可选配置回调，覆盖默认错误码和消息</param>
    public static IServiceCollection AddTaiPiExceptionHandling(
        this IServiceCollection services,
        Action<ExceptionHandlingOptions>? configure = null)
    {
        var options = new ExceptionHandlingOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        return services;
    }

    /// <summary>
    /// 使用全局异常处理中间件
    /// </summary>
    public static IApplicationBuilder UseTaiPiExceptionHandling(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
