using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IO;

namespace Taipi.Core.Middleware;

/// <summary>
/// 请求日志中间件配置项
/// </summary>
public class RequestLoggingOptions
{
    /// <summary>需要跳过日志的路径前缀（大小写不敏感）</summary>
    public string[] IgnoredPathPrefixes { get; set; } = { "/health", "/swagger" };

    /// <summary>静态文件目录（路径包含这些字符串即认为属于静态资源）</summary>
    public string[] StaticFileDirectories { get; set; } = { "/static/", "/wwwroot/", "/lib/", "/css/", "/js/" };

    /// <summary>在上述静态目录中，匹配这些扩展名的文件跳过日志</summary>
    public string[] IgnoredFileExtensions { get; set; } = { ".js", ".css", ".map", ".ico", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".woff", ".woff2", ".ttf", ".eot" };

    /// <summary>请求体最大记录长度（字符数），超出部分截断</summary>
    public int MaxRequestBodyLength { get; set; } = 4096;

    /// <summary>响应体最大记录长度（用于业务错误检测），超出部分截断</summary>
    public int MaxResponseBodyLength { get; set; } = 1024;

    /// <summary>是否启用请求体记录（生产环境建议设为 false）</summary>
    public bool LogRequestBodyEnabled { get; set; } = false;

    /// <summary>是否启用响应体记录（用于检测业务错误）</summary>
    public bool LogResponseBodyForErrorDetection { get; set; } = true;

    /// <summary>需要脱敏的字段名（QueryString 和 JSON Body）</summary>
    public string[] SensitiveFields { get; set; } = { "password", "token", "secret", "authorization", "apikey" };

    /// <summary>脱敏替换字符串</summary>
    public string SensitiveReplacement { get; set; } = "***";

    /// <summary>业务响应中表示成功的 code 值集合（不区分大小写），支持字符串或数字</summary>
    public HashSet<string> SuccessCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase) { "0", "200", "success" };

    /// <summary>业务错误时使用的日志级别（HTTP 200 但 code 非成功码）</summary>
    public LogLevel BusinessErrorLogLevel { get; set; } = LogLevel.Warning;

    /// <summary>响应体中表示业务状态码的字段名，默认 "code"</summary>
    public string BusinessCodeFieldName { get; set; } = "code";

    /// <summary>响应体中表示业务消息的字段名，默认 "message"</summary>
    public string BusinessMessageFieldName { get; set; } = "message";
}

/// <summary>
/// 请求日志中间件：一行输出一个请求的完整信息（方法、路径、参数、状态码、耗时）
/// 支持过滤低价值请求、请求体记录、敏感信息脱敏、根据业务错误码调整日志级别
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly RequestLoggingOptions _options;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        IOptions<RequestLoggingOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        if (ShouldSkip(path))
        {
            await _next(context);
            return;
        }

        var method = context.Request.Method;
        var rawQuery = context.Request.QueryString.Value ?? "";
        var queryString = SanitizeQueryString(rawQuery);

        string requestBody = "";
        if (_options.LogRequestBodyEnabled && ShouldReadBody(context))
        {
            requestBody = await ReadRequestBodyAsync(context);
            if (!string.IsNullOrEmpty(requestBody))
                requestBody = SanitizeBody(requestBody);
        }

        Stream? originalResponseBody = null;
        MemoryStream? responseCaptureStream = null;
        string responseBody = "";

        if (_options.LogResponseBodyForErrorDetection)
        {
            originalResponseBody = context.Response.Body;
            responseCaptureStream = _recyclableMemoryStreamManager.GetStream();
            context.Response.Body = responseCaptureStream;
        }

        var stopwatch = Stopwatch.StartNew();
        Exception? caughtException = null;
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            caughtException = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            if (responseCaptureStream != null && originalResponseBody != null)
            {
                responseCaptureStream.Position = 0;
                using var reader = new StreamReader(responseCaptureStream, Encoding.UTF8, leaveOpen: false);
                responseBody = await reader.ReadToEndAsync();
                if (!string.IsNullOrEmpty(responseBody) && responseBody.Length > _options.MaxResponseBodyLength)
                    responseBody = responseBody[.._options.MaxResponseBodyLength] + "...(truncated)";

                responseCaptureStream.Position = 0;
                await responseCaptureStream.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;
            }

            var statusCode = context.Response.StatusCode;
            var logLevel = GetLogLevel(statusCode, responseBody, caughtException);

            if (!string.IsNullOrEmpty(responseBody) && IsBusinessError(responseBody, out var errorCode, out var errorMsg))
            {
                _logger.Log(logLevel,
                    "{Method} {Path}{Query} → {StatusCode} (业务错误: {ErrorCode} - {ErrorMsg}) ({ElapsedMs}ms) [Resp: {ResponseBody}]",
                    method, path, queryString, statusCode, errorCode, errorMsg, stopwatch.ElapsedMilliseconds, responseBody);
            }
            else if (!string.IsNullOrEmpty(requestBody))
            {
                _logger.Log(logLevel,
                    "{Method} {Path}{Query} [Req: {RequestBody}] → {StatusCode} ({ElapsedMs}ms)",
                    method, path, queryString, requestBody, statusCode, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.Log(logLevel,
                    "{Method} {Path}{Query} → {StatusCode} ({ElapsedMs}ms)",
                    method, path, queryString, statusCode, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    private LogLevel GetLogLevel(int statusCode, string responseBody, Exception? exception)
    {
        if (exception != null)
            return LogLevel.Error;
        if (statusCode >= 500)
            return LogLevel.Error;
        if (statusCode >= 400)
            return LogLevel.Warning;
        if (statusCode is >= 200 and < 400 && !string.IsNullOrEmpty(responseBody))
        {
            if (IsBusinessError(responseBody, out _, out _))
                return _options.BusinessErrorLogLevel;
        }
        return LogLevel.Information;
    }

    private bool IsBusinessError(string responseBody, out string? errorCode, out string? errorMessage)
    {
        errorCode = null;
        errorMessage = null;

        if (string.IsNullOrEmpty(responseBody))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty(_options.BusinessCodeFieldName, out var codeElement))
            {
                string code;
                if (codeElement.ValueKind == JsonValueKind.Number)
                    code = codeElement.GetRawText(); // 数字转字符串，如 0 -> "0"
                else if (codeElement.ValueKind == JsonValueKind.String)
                    code = codeElement.GetString() ?? "";
                else
                    code = codeElement.GetRawText();

                if (!string.IsNullOrEmpty(code) && !_options.SuccessCodes.Contains(code))
                {
                    errorCode = code;
                    if (doc.RootElement.TryGetProperty(_options.BusinessMessageFieldName, out var msgElement))
                        errorMessage = msgElement.GetString();
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            // 不是合法 JSON，忽略
        }
        return false;
    }

    /// <summary>
    /// 判断是否应该跳过日志记录
    /// </summary>
    /// <param name="path">请求路径</param>
    /// <returns>是否应该跳过</returns>
    private bool ShouldSkip(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        foreach (var prefix in _options.IgnoredPathPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        bool insideStaticDir = _options.StaticFileDirectories.Any(dir =>
            path.Contains(dir, StringComparison.OrdinalIgnoreCase));

        if (insideStaticDir)
        {
            foreach (var ext in _options.IgnoredFileExtensions)
            {
                if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断是否应该读取请求体
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>是否应该读取请求体</returns>
    private static bool ShouldReadBody(HttpContext context)
    {
        var method = context.Request.Method;
        if (!(HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method)))
            return false;

        var contentType = context.Request.ContentType;
        if (string.IsNullOrEmpty(contentType))
            return false;

        if (contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return true;
    }


    private async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        try
        {
            context.Request.EnableBuffering();
            context.Request.Body.Position = 0;

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true, bufferSize: 512);
            var buffer = new char[_options.MaxRequestBodyLength];
            var readLength = await reader.ReadAsync(buffer, 0, _options.MaxRequestBodyLength);
            var body = new string(buffer, 0, readLength);

            // 判断是否还有更多内容，直接通过 ReadAsync 是否已读取全部判断
            // 如果读取的长度等于缓冲区长度，可能存在更多数据，但为避免性能问题，不再深层检测，直接标记截断
            if (readLength == _options.MaxRequestBodyLength)
            {
                body += "...(truncated)";
            }

            context.Request.Body.Position = 0;
            return string.IsNullOrWhiteSpace(body) ? "" : body;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取请求体失败: {Path}", context.Request.Path);
            return "";
        }
    }
    private string SanitizeQueryString(string query)
    {
        if (string.IsNullOrEmpty(query))
            return query;

        string result = query;
        foreach (var field in _options.SensitiveFields)
        {
            var pattern = $@"([?&]){Regex.Escape(field)}=[^&]*";
            var replacement = $"$1{field}={_options.SensitiveReplacement}";
            result = Regex.Replace(result, pattern, replacement, RegexOptions.IgnoreCase);
        }
        return result;
    }

    private string SanitizeBody(string body)
    {
        if (string.IsNullOrEmpty(body))
            return body;

        string result = body;
        foreach (var field in _options.SensitiveFields)
        {
            // JSON 双引号形式: "password":"123" 或 "password": "123"
            var pattern = $@"(""{Regex.Escape(field)}""\s*:\s*)""[^""]*""";
            var replacement = $"$1\"{_options.SensitiveReplacement}\"";
            result = Regex.Replace(result, pattern, replacement, RegexOptions.IgnoreCase);
            // 值不带引号的情况（如 {"password":123}）
            pattern = $@"(""{Regex.Escape(field)}""\s*:\s*)[^,\s}}]+";
            replacement = $"$1{_options.SensitiveReplacement}";
            result = Regex.Replace(result, pattern, replacement, RegexOptions.IgnoreCase);
        }
        return result;
    }
}