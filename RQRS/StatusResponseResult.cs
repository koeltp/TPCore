namespace Taipi.Core.RQRS;

/// <summary>
/// 状态响应结果类，包含状态码、消息和时间戳
/// </summary>
public class StatusResponseResult
{
    public int Code { get; set; } = 200;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;


    /// <summary>
    /// 成功响应结果，默认状态码为200，消息由调用者提供
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static StatusResponseResult Success(string message)
    {
        return new StatusResponseResult { Code = 200, Message = message };
    }

    /// <summary>
    /// 错误响应结果，状态码由调用者提供，消息由调用者提供
    /// </summary>
    /// <param name="code"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public static StatusResponseResult Error(int code, string message)
    {
        return new StatusResponseResult { Code = code, Message = message };
    }

    /// <summary>
    /// 成功响应结果，默认状态码为200，默认消息为"操作成功"
    /// </summary>
    /// <returns></returns>
    public static StatusResponseResult Success()
    {
        return Success("操作成功");
    }

    /// <summary>
    /// 请求参数错误响应结果，状态码为400，消息由调用者提供或默认为"请求参数错误"
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static StatusResponseResult BadRequest(string message = "请求参数错误")
    {
        return Error(400, message);
    }

    /// <summary>
    /// 未授权响应结果，状态码为401，消息由调用者提供或默认为"未授权"
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static StatusResponseResult Unauthorized(string message = "未授权")
    {
        return Error(401, message);
    }

    /// <summary>
    /// 禁止访问响应结果，状态码为403，消息由调用者提供或默认为"禁止访问"
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static StatusResponseResult Forbidden(string message = "禁止访问")
    {
        return Error(403, message);
    }

    /// <summary>
    /// 资源未找到响应结果，状态码为404，消息由调用者提供或默认为"资源未找到"
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static StatusResponseResult NotFound(string message = "资源未找到")
    {
        return Error(404, message);
    }

    /// <summary>
    /// 服务器内部错误响应结果，状态码为500，消息由调用者提供或默认为"服务器内部错误"
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static StatusResponseResult InternalError(string message = "服务器内部错误")
    {
        return Error(500, message);
    }
}