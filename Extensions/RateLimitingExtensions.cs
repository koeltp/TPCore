using System.Net;
using System.Threading.RateLimiting;

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
                var clientIp = GetClientIp(context, options);
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
                var clientIp = GetClientIp(context, options);
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
                var clientIp = GetClientIp(context, options);
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
                var clientIp = GetClientIp(context, options);
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
    /// 获取客户端真实 IP。仅在请求来自受信代理时才读取 X-Forwarded-For，
    /// 防止攻击者伪造该头绕过 IP 限流。
    /// 自动处理 IPv4-mapped IPv6 地址（::ffff:x.x.x.x → x.x.x.x）
    /// </summary>
    private static string GetClientIp(HttpContext context, RateLimitingOptions options)
    {
        var remoteIp = context.Connection.RemoteIpAddress;

        // 将 IPv4-mapped IPv6 地址映射回 IPv4，确保双栈环境下代理识别正确
        if (remoteIp != null && remoteIp.IsIPv4MappedToIPv6)
            remoteIp = remoteIp.MapToIPv4();

        // 仅当 RemoteIpAddress 属于受信代理时，才信任 X-Forwarded-For
        if (remoteIp != null && (options.KnownProxies.Count > 0 || options.KnownNetworks.Count > 0))
        {
            var isTrusted = options.KnownProxies.Any(proxy => proxy.Equals(remoteIp));

            // 也检查 KnownNetworks
            if (!isTrusted)
            {
                isTrusted = options.KnownNetworks.Any(network => network.Contains(remoteIp));
            }

            if (isTrusted)
            {
                var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    return forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .First().Trim();
                }
            }
        }

        return remoteIp?.ToString() ?? "unknown";
    }
}

/// <summary>
/// 速率限制策略名称常量
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>Token 端点策略名</summary>
    public const string TokenEndpoint = "token_endpoint";

    /// <summary>登录端点策略名</summary>
    public const string LoginEndpoint = "login_endpoint";

    /// <summary>外部登录端点策略名</summary>
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

    /// <summary>
    /// 受信代理 IP 列表。仅来自这些 IP 的请求才会读取 X-Forwarded-For 头。
    /// 为空时（默认）不信任任何代理，直接使用 RemoteIpAddress，防止 IP 伪造绕过限流
    /// </summary>
    public List<IPAddress> KnownProxies { get; set; } = [];

    /// <summary>
    /// 受信网络列表（CIDR 格式）。仅来自这些网络的请求才会读取 X-Forwarded-For 头
    /// </summary>
    public List<IPNetwork> KnownNetworks { get; set; } = [];
}

/// <summary>
/// 表示一个 IP 网络段（CIDR），用于判断某个 IP 是否属于受信网络
/// </summary>
public class IPNetwork
{
    /// <summary>网络地址</summary>
    public IPAddress Address { get; }

    /// <summary>前缀长度（CIDR 中的 /n）</summary>
    public int PrefixLength { get; }

    private readonly byte[] _networkBytes;

    /// <summary>
    /// 创建 IP 网络段
    /// </summary>
    /// <param name="address">网络地址</param>
    /// <param name="prefixLength">前缀长度（如 24 表示 /24）</param>
    public IPNetwork(IPAddress address, int prefixLength)
    {
        Address = address;
        PrefixLength = prefixLength;
        _networkBytes = address.GetAddressBytes();
    }

    /// <summary>
    /// 判断指定 IP 是否属于本网络段。
    /// 支持 IPv4-mapped IPv6 地址（::ffff:x.x.x.x）自动映射到 IPv4 比较
    /// </summary>
    public bool Contains(IPAddress ip)
    {
        // 将 IPv4-mapped IPv6 地址映射回 IPv4，确保双栈环境下匹配正确
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();

        var ipBytes = ip.GetAddressBytes();
        if (ipBytes.Length != _networkBytes.Length) return false;

        var fullBytes = PrefixLength / 8;
        var remainingBits = PrefixLength % 8;

        for (var i = 0; i < fullBytes; i++)
        {
            if (ipBytes[i] != _networkBytes[i]) return false;
        }

        if (remainingBits > 0 && fullBytes < _networkBytes.Length)
        {
            var mask = (byte)(0xFF << (8 - remainingBits));
            if ((ipBytes[fullBytes] & mask) != (_networkBytes[fullBytes] & mask)) return false;
        }

        return true;
    }
}
