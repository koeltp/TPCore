namespace Taipi.Core.Middleware;

/// <summary>
/// CorrelationId 中间件配置选项
/// </summary>
public class CorrelationIdOptions
{
    /// <summary>
    /// 请求头名称，用于读取上游传入的 CorrelationId。默认 "X-Correlation-Id"
    /// </summary>
    public string HeaderName { get; set; } = "X-Correlation-Id";

    /// <summary>
    /// 是否将 CorrelationId 回写到响应头中，便于前端追踪。默认 true
    /// </summary>
    public bool IncludeInResponse { get; set; } = true;

    /// <summary>
    /// 自动生成的 CorrelationId 长度（取 Guid 前 N 位）。默认 16，范围 8-32
    /// </summary>
    public int GenerateIdLength { get; set; } = 16;

    /// <summary>
    /// 允许的 CorrelationId 最大长度，超过此长度视为非法并生成新 ID。默认 64
    /// </summary>
    public int MaxIdLength { get; set; } = 64;
}
