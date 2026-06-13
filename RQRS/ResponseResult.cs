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
    /// 通过初始数据构造响应结果，Code 默认为 0（成功）
    /// </summary>
    /// <param name="data">要携带的业务数据</param>
    public ResponseResult(T data) { Data = data; }

    /// <summary>
    /// 成功响应，携带业务数据
    /// </summary>
    /// <param name="data">要返回的业务数据</param>
    public static ResponseResult<T> Success(T data)
    {
        return new ResponseResult<T>(data) { Message = "操作成功" };
    }

    /// <summary>
    /// 成功响应，携带业务数据并使用自定义消息
    /// </summary>
    /// <param name="data">要返回的业务数据</param>
    /// <param name="message">成功提示信息</param>
    public static ResponseResult<T> Success(T data, string message)
    {
        return new ResponseResult<T>(data) { Message = message };
    }

    /// <summary>
    /// 错误响应，业务错误码和消息由调用者指定
    /// </summary>
    public static new ResponseResult<T> Error(int code, string message)
    {
        return new ResponseResult<T> { Code = code, Message = message };
    }
}
