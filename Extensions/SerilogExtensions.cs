using Serilog;
using Taipi.Core.Middleware;

namespace Taipi.Core.Extensions;

/// <summary>
/// Serilog 日志配置扩展方法
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// 创建 Serilog 引导日志，用于捕获 Host 构建前的启动错误
    /// </summary>
    public static void CreateBootstrapLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
                .Build())
            .Enrich.FromLogContext()
            .CreateBootstrapLogger();
    }

    /// <summary>
    /// 配置 Host 使用 Serilog，从 appsettings 读取完整配置
    /// </summary>
    public static IHostBuilder UseSerilogFromConfiguration(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, services, loggerConfig) =>
        {
            loggerConfig.ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .Enrich.FromLogContext();
        });
    }

    /// <summary>
    /// 注册请求日志中间件（一行输出一个请求，自动过滤低价值路径）
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }

    /// <summary>
    /// 注册 CorrelationId 中间件，将请求链路标识注入 Serilog 日志上下文
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
