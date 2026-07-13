using Serilog;
using Taipi.Core.Logging;
namespace Taipi.Core.Extensions;

/// <summary>
/// Serilog 日志配置扩展方法
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// 创建 Serilog 引导日志，用于捕获 Host 构建前的启动错误。
    /// 如果配置文件加载失败，降级为控制台输出。
    /// </summary>
    public static void CreateBootstrapLogger()
    {
        try
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateBootstrapLogger();
        }
        catch
        {
            // 降级：确保引导日志至少能输出到控制台
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateBootstrapLogger();
        }
    }

    /// <summary>
    /// 配置 Host 使用 Serilog，从 appsettings 和 DI 容器读取完整配置。
    /// 已集成 FromLogContext 和全局脱敏（SensitiveDataEnricher）
    /// </summary>
    public static IHostBuilder UseSerilogFromConfiguration(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, services, loggerConfig) =>
        {
            loggerConfig
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.With<SensitiveDataEnricher>();
        });
    }

}
