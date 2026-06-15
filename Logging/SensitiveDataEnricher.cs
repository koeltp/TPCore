using Serilog.Core;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace Taipi.Core.Logging;

/// <summary>
/// 全局日志脱敏 Enricher：自动识别并替换常见的敏感字段值
/// </summary>
public class SensitiveDataEnricher : ILogEventEnricher
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "pwd", "passwd",
        "token", "access_token", "refresh_token",
        "secret", "clientsecret", "client_secret",
        "apikey", "api_key",
        "authorization", "auth",
        "hash", "hashed", "salt",
        "creditcard", "cardnumber",
        "connectionstring"  // 可选，防止连接字符串泄露
    };

    private const string Replacement = "***";
    private static readonly Regex ValueReplacementRegex;

    static SensitiveDataEnricher()
    {
        // 构建一个正则：匹配 (敏感字段名=值) 或 ("敏感字段名":"值") 等形式
        // 支持: key=value, key="value", key:'value', key:value (JSON无引号)
        var pattern = $@"(?<prefix>(?i)(?:{string.Join("|", SensitiveKeys)})\s*[:=]\s*)(?<value>['""]?[^'""\s,}}]+)";
        ValueReplacementRegex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // 1. 处理消息模板中的纯文本内容（比如 Log.Information("... password=xxx")）
        var rawMessage = logEvent.RenderMessage();
        var sanitizedMessage = SanitizeText(rawMessage);
        if (sanitizedMessage != rawMessage)
        {
            // 替换消息文本（通过修改属性）
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("__SanitizedMessage", sanitizedMessage));
            // 注意：Serilog 的 Message 是只读的，这里添加自定义属性供输出模板使用
            // 更好的做法是使用自定义的 ITextFormatter，但简单场景下添加额外属性即可。
        }

        // 2. 处理所有结构化属性（如 Log.Information("User {User}", user)）
        foreach (var property in logEvent.Properties.ToList())
        {
            if (property.Value is ScalarValue scalar && scalar.Value is string stringValue)
            {
                var sanitized = SanitizeText(stringValue);
                if (sanitized != stringValue)
                {
                    logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(property.Key, sanitized));
                }
            }
            // 可选：处理嵌套对象（如匿名对象、字典等），这里为了性能暂不递归
        }
    }

    private static string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // 替换所有匹配的敏感值
        return ValueReplacementRegex.Replace(text, match =>
        {
            var prefix = match.Groups["prefix"].Value;
            var value = match.Groups["value"].Value;
            // 保留引号风格
            if (value.StartsWith("\"") && value.EndsWith("\""))
                return $"{prefix}\"{Replacement}\"";
            if (value.StartsWith("'") && value.EndsWith("'"))
                return $"{prefix}'{Replacement}'";
            return $"{prefix}{Replacement}";
        });
    }
}