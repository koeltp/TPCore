using System.Diagnostics;
using System.Text;

namespace Taipi.Core.Middleware;

/// <summary>
/// 请求日志中间件：一行输出一个请求的完整信息（方法、路径、参数、状态码、耗时）
/// 自动过滤健康检查和静态文件等低价值请求
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    // 不记录日志的路径前缀
    private static readonly string[] _ignoredPrefixes = ["/health", "/swagger"];
    // 不记录日志的路径后缀（静态文件）
    private static readonly string[] _ignoredExtensions =
        [".js", ".css", ".map", ".ico", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".woff", ".woff2", ".ttf", ".eot"];
    // 请求体最大读取长度，避免大文件上传撑爆日志
    private const int MaxBodyLength = 4096;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        // 过滤低价值请求，避免日志噪音
        if (ShouldSkip(path))
        {
            await _next(context);
            return;
        }

        var method = context.Request.Method;
        var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";

        // 读取请求体（仅对有 Body 的请求方法，且非文件上传）
        var body = "";
        if (HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method))
        {
            if (!IsFileUpload(context))
            {
                body = await ReadRequestBodyAsync(context);
            }
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var statusCode = context.Response.StatusCode;
            var level = statusCode >= 500 ? LogLevel.Error
                      : statusCode >= 400 ? LogLevel.Warning
                      : LogLevel.Information;

            if (string.IsNullOrEmpty(body))
            {
                _logger.Log(level, "{Method} {Path}{Query} → {StatusCode} ({ElapsedMs}ms)",
                    method, path, queryString, statusCode, sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.Log(level, "{Method} {Path}{Query} {Body} → {StatusCode} ({ElapsedMs}ms)",
                    method, path, queryString, body, statusCode, sw.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// 读取请求体内容，启用缓冲以确保后续管道仍可读取
    /// </summary>
    private static async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        if (body.Length > MaxBodyLength)
        {
            body = body[..MaxBodyLength] + "...(truncated)";
        }

        return string.IsNullOrWhiteSpace(body) ? "" : body;
    }

    /// <summary>
    /// 判断请求路径是否应跳过日志记录
    /// </summary>
    private static bool ShouldSkip(string? path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        foreach (var prefix in _ignoredPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var ext in _ignoredExtensions)
        {
            if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 判断是否为文件上传请求，避免将二进制内容写入日志
    /// </summary>
    private static bool IsFileUpload(HttpContext context)
    {
        var contentType = context.Request.ContentType;
        if (string.IsNullOrEmpty(contentType)) return false;

        return contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase)
            || contentType.StartsWith("application/octet-stream", StringComparison.OrdinalIgnoreCase);
    }
}
