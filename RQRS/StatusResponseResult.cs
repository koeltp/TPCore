namespace Taipi.Core.RQRS;

/// <summary>
/// 状态响应结果基类，Code=0 表示成功，非0 为业务错误码
/// </summary>
public class StatusResponseResult
{
    /// <summary>业务状态码，0=成功，非0=业务错误码（4位：模块+编号）</summary>
    public int Code { get; set; } = 0;

    /// <summary>响应消息描述</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 请求追踪标识，用于关联日志链路。
    /// 仅在异常响应中由中间件赋值，正常业务响应为 null
    /// </summary>
    public string? CorrelationId { get; set; }


    /// <summary>
    /// 成功响应，默认消息为"操作成功"
    /// </summary>
    public static StatusResponseResult Success()
    {
        return new StatusResponseResult { Message = "操作成功" };
    }

    /// <summary>
    /// 成功响应，自定义消息
    /// </summary>
    public static StatusResponseResult Success(string message)
    {
        return new StatusResponseResult { Message = message };
    }

    /// <summary>
    /// 错误响应，业务错误码和消息由调用者指定
    /// </summary>
    public static StatusResponseResult Error(int code, string message)
    {
        return new StatusResponseResult { Code = code, Message = message };
    }
}
