using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Taipi.Core.Extensions;

/// <summary>
/// 速率限制扩展方法，基于 ASP.NET Core 内置的 RateLimiting 中间件
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// 注册速率限制策略，包含全局默认策略和可选的端点专属策略
    /// 配置通过 appsettings.json 的 RateLimiting 节读取
    /// </summary>
    public static IServiceCollection AddTaiPiRateLimiting(
        this IServiceCollection services,
        Action<RateLimitingOptions>? configure = null)
    {
        var options = new RateLimitingOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.AddRateLimiter(limiterOptions =>
        {
            // 全局默认策略：每 IP 滑动窗口限流
            limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var clientIp = GetClientIp(context);
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = options.GlobalPermitLimit,
                        Window = TimeSpan.FromSeconds(options.GlobalWindowSeconds),
                        SegmentsPerWindow = options.GlobalSegments,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // Token 端点策略：严格限流，防暴力破解
            limiterOptions.AddPolicy(RateLimitPolicies.TokenEndpoint, context =>
            {
                var clientIp = GetClientIp(context);
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = options.TokenPermitLimit,
                        Window = TimeSpan.FromSeconds(options.TokenWindowSeconds),
                        SegmentsPerWindow = options.TokenSegments,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // 登录端点策略：严格限流，防暴力破解
            limiterOptions.AddPolicy(RateLimitPolicies.LoginEndpoint, context =>
            {
                var clientIp = GetClientIp(context);
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = options.LoginPermitLimit,
                        Window = TimeSpan.FromSeconds(options.LoginWindowSeconds),
                        SegmentsPerWindow = options.LoginSegments,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // 外部登录端点策略：防 OAuth 滥用
            limiterOptions.AddPolicy(RateLimitPolicies.ExternalLoginEndpoint, context =>
            {
                var clientIp = GetClientIp(context);
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = options.ExternalLoginPermitLimit,
                        Window = TimeSpan.FromSeconds(options.ExternalLoginWindowSeconds),
                        SegmentsPerWindow = options.ExternalLoginSegments,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // 被限流时的响应
            limiterOptions.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    """{"code":429,"message":"请求过于频繁，请稍后再试"}""", cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// 获取客户端真实 IP，优先使用 X-Forwarded-For（反向代理场景）
    /// </summary>
    private static string GetClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For 可能包含多个 IP，取第一个（最原始的客户端 IP）
            return forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .First().Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// 速率限制策略名称常量
/// </summary>
public static class RateLimitPolicies
{
    public const string TokenEndpoint = "token_endpoint";
    public const string LoginEndpoint = "login_endpoint";
    public const string ExternalLoginEndpoint = "external_login_endpoint";
}

/// <summary>
/// 速率限制配置选项
/// </summary>
public class RateLimitingOptions
{
    /// <summary>全局：窗口内最大请求数，默认 100</summary>
    public int GlobalPermitLimit { get; set; } = 100;

    /// <summary>全局：滑动窗口秒数，默认 60</summary>
    public int GlobalWindowSeconds { get; set; } = 60;

    /// <summary>全局：滑动窗口分段数，默认 4</summary>
    public int GlobalSegments { get; set; } = 4;

    /// <summary>Token 端点：窗口内最大请求数，默认 10</summary>
    public int TokenPermitLimit { get; set; } = 10;

    /// <summary>Token 端点：滑动窗口秒数，默认 60</summary>
    public int TokenWindowSeconds { get; set; } = 60;

    /// <summary>Token 端点：滑动窗口分段数，默认 4</summary>
    public int TokenSegments { get; set; } = 4;

    /// <summary>登录端点：窗口内最大请求数，默认 5</summary>
    public int LoginPermitLimit { get; set; } = 5;

    /// <summary>登录端点：滑动窗口秒数，默认 60</summary>
    public int LoginWindowSeconds { get; set; } = 60;

    /// <summary>登录端点：滑动窗口分段数，默认 4</summary>
    public int LoginSegments { get; set; } = 4;

    /// <summary>外部登录端点：窗口内最大请求数，默认 5</summary>
    public int ExternalLoginPermitLimit { get; set; } = 5;

    /// <summary>外部登录端点：滑动窗口秒数，默认 60</summary>
    public int ExternalLoginWindowSeconds { get; set; } = 60;

    /// <summary>外部登录端点：滑动窗口分段数，默认 4</summary>
    public int ExternalLoginSegments { get; set; } = 4;
}
