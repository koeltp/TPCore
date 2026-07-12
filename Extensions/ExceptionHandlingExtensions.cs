using Taipi.Core.Middleware;
using Taipi.Core.Exceptions;
using Taipi.Core.Exceptions.Abstract;

namespace Taipi.Core.Extensions;

/// <summary>
/// 异常处理中间件服务扩展方法
/// </summary>
public static class ExceptionHandlingExtensions
{
    /// <summary>
    /// 注册全局异常处理配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">可选配置回调，覆盖默认错误码和消息</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddTaiPiExceptionHandling(
        this IServiceCollection services,
        Action<ExceptionHandlingOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);
        else
            services.Configure<ExceptionHandlingOptions>(_ => { });

        services.AddScoped<IExceptionHandler<AppException>, AppExceptionHandler>();
        services.AddScoped<IExceptionHandler<ValidationException>, ValidationHandler>();        
        services.AddScoped<IExceptionHandler<ForbiddenException>, ForbiddenHandler>();
        services.AddScoped<IExceptionHandler<Exception>, UnknownExceptionHandler>();
        return services;
    }



    /// <summary>
    /// 使用全局异常处理中间件
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器</returns>
    public static IApplicationBuilder UseTaiPiExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}