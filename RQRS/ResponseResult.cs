namespace Taipi.Core.RQRS;

/// <summary>
/// 泛型响应结果类，在状态响应基础上携带业务数据
/// </summary>
/// <typeparam name="T">响应数据的类型</typeparam>
public class ResponseResult<T> : StatusResponseResult
{
    /// <summary>
    /// 响应中携带的业务数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 无参构造函数，用于反序列化或后续手动赋值场景
    /// </summary>
    public ResponseResult() { }

    /// <summary>
    /// 通过初始数据构造响应结果，Code 默认为 200
    /// </summary>
    /// <param name="data">要携带的业务数据</param>
    public ResponseResult(T data) { Data = data; }

    /// <summary>
    /// 成功响应（状态码200），携带业务数据
    /// </summary>
    /// <param name="data">要返回的业务数据</param>
    public static ResponseResult<T> Success(T data)
    {
        return new ResponseResult<T>(data) { Message = "操作成功" };
    }

    /// <summary>
    /// 成功响应（状态码200），携带业务数据并使用自定义消息
    /// </summary>
    /// <param name="data">要返回的业务数据</param>
    /// <param name="message">成功提示信息</param>
    public static ResponseResult<T> Success(T data, string message)
    {
        return new ResponseResult<T>(data) { Message = message };
    }

    /// <summary>
    /// 错误响应，状态码与消息均由调用者指定
    /// </summary>
    /// <param name="code">错误状态码</param>
    /// <param name="message">错误描述信息</param>
    public static new ResponseResult<T> Error(int code, string message)
    {
        return new ResponseResult<T> { Code = code, Message = message };
    }

    /// <summary>
    /// 请求参数错误（400），默认消息为"请求参数错误"
    /// </summary>
    public static new ResponseResult<T> BadRequest(string message = "请求参数错误")
    {
        return Error(400, message);
    }

    /// <summary>
    /// 未授权（401），默认消息为"未授权"
    /// </summary>
    public static new ResponseResult<T> Unauthorized(string message = "未授权")
    {
        return Error(401, message);
    }

    /// <summary>
    /// 禁止访问（403），默认消息为"禁止访问"
    /// </summary>
    public static new ResponseResult<T> Forbidden(string message = "禁止访问")
    {
        return Error(403, message);
    }

    /// <summary>
    /// 资源未找到（404），默认消息为"资源未找到"
    /// </summary>
    public static new ResponseResult<T> NotFound(string message = "资源未找到")
    {
        return Error(404, message);
    }

    /// <summary>
    /// 服务器内部错误（500），默认消息为"服务器内部错误"
    /// </summary>
    public static new ResponseResult<T> InternalError(string message = "服务器内部错误")
    {
        return Error(500, message);
    }
}
