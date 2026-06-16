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
        // 1. 处理所有结构化属性：属性名匹配敏感字段时直接替换值
        foreach (var property in logEvent.Properties.ToList())
        {
            if (property.Value is ScalarValue scalar && scalar.Value is string stringValue)
            {
                // 属性名匹配敏感字段 → 直接替换整个值为 ***
                if (SensitiveKeys.Contains(property.Key) && !string.IsNullOrEmpty(stringValue))
                {
                    logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(property.Key, Replacement));
                    continue;
                }
                // 属性值文本中包含 key=value 形式的敏感信息 → 替换值中的敏感部分
                var sanitized = SanitizeText(stringValue);
                if (sanitized != stringValue)
                {
                    logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(property.Key, sanitized));
                }
            }
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