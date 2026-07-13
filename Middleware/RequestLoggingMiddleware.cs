using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using Taipi.Core.Exceptions.Abstract;

namespace Taipi.Core.Middleware;

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

    /// <summary>
    /// 预编译的查询参数脱敏正则缓存
    /// </summary>
    private readonly (Regex Regex, string Replacement)[] _queryStringSanitizers;

    /// <summary>
    /// 预编译的请求体脱敏正则缓存（每个字段两个正则：引号值和非引号值）
    /// </summary>
    private readonly (Regex Regex, string Replacement)[][] _bodySanitizers;

    /// <summary>
    /// 创建请求日志中间件实例
    /// </summary>
    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        IOptions<RequestLoggingOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

        // 预编译脱敏正则，避免每次请求重复编译
        _queryStringSanitizers = _options.SensitiveFields
            .Select(f => (new Regex($@"([?&]){Regex.Escape(f)}=[^&]*", RegexOptions.Compiled | RegexOptions.IgnoreCase), $"$1{f}={_options.SensitiveReplacement}"))
            .ToArray();

        _bodySanitizers = _options.SensitiveFields
            .Select(f => new[]
            {
                // JSON 双引号形式: "password":"123" 或 "password": "123"
                (new Regex($@"(""{Regex.Escape(f)}""\s*:\s*)""[^""]*""", RegexOptions.Compiled | RegexOptions.IgnoreCase), $"$1\"{_options.SensitiveReplacement}\""),
                // 值不带引号的情况（如 {"password":123}）
                (new Regex($@"(""{Regex.Escape(f)}""\s*:\s*)[^,\s}}]+", RegexOptions.Compiled | RegexOptions.IgnoreCase), $"$1{_options.SensitiveReplacement}")
            })
            .ToArray();
    }

    /// <summary>
    /// 处理 HTTP 请求，记录请求日志
    /// </summary>
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
            var logLevel = GetLogLevel(statusCode, responseBody, caughtException, context);

            if (!string.IsNullOrEmpty(responseBody) && IsBusinessError(responseBody, out var errorCode, out var errorMsg))
            {
                _logger.Log(logLevel,
                    "{Method} {Path}{Query} → {StatusCode} (业务错误: {ErrorCode} - {ErrorMsg}) ({ElapsedMs}ms)",
                    method, path, queryString, statusCode, errorCode, errorMsg, stopwatch.ElapsedMilliseconds);
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

    private LogLevel GetLogLevel(int statusCode, string responseBody, Exception? exception, HttpContext context)
    {
        // 1. 异常被本中间件直接捕获（异常处理中间件未介入），委托给 Handler 决定日志级别
        if (exception != null)
            return GetExceptionLogLevel(exception, context.RequestServices);

        // 2. 异常已被异常处理中间件处理，通过 HttpContext.Items 传递日志级别
        if (context.Items["ExceptionLogLevel"] is LogLevel handlerLogLevel)
            return handlerLogLevel;

        // 3. 无异常，按状态码判断
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

    /// <summary>
    /// 委托给 <see cref="ExceptionHandlerDelegateCache"/> 获取异常日志级别，与异常处理中间件保持一致
    /// </summary>
    private static LogLevel GetExceptionLogLevel(Exception exception, IServiceProvider serviceProvider)
    {
        return ExceptionHandlerDelegateCache.GetLogLevel(exception, serviceProvider);
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
                    code = codeElement.GetRawText();
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

            if (readLength == _options.MaxRequestBodyLength)
                body += "...(truncated)";

            context.Request.Body.Position = 0;
            return string.IsNullOrWhiteSpace(body) ? "" : body;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取请求体失败: {Path}", context.Request.Path);
            return "";
        }
    }

    /// <summary>
    /// 使用预编译正则对查询参数中的敏感字段脱敏
    /// </summary>
    private string SanitizeQueryString(string query)
    {
        if (string.IsNullOrEmpty(query))
            return query;

        var result = query;
        foreach (var (regex, replacement) in _queryStringSanitizers)
            result = regex.Replace(result, replacement);
        return result;
    }

    /// <summary>
    /// 使用预编译正则对请求体中的敏感字段脱敏
    /// </summary>
    private string SanitizeBody(string body)
    {
        if (string.IsNullOrEmpty(body))
            return body;

        var result = body;
        foreach (var sanitizers in _bodySanitizers)
        {
            foreach (var (regex, replacement) in sanitizers)
                result = regex.Replace(result, replacement);
        }
        return result;
    }
}
