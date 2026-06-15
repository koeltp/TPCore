
namespace Taipi.Core.Middleware;

/// <summary>
/// 请求日志记录中间件选项
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
