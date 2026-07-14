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
    /// 通过初始数据构造响应结果，Code 默认为 0（成功），Message 默认为"操作成功"
    /// </summary>
    /// <param name="data">要携带的业务数据</param>
    public ResponseResult(T data) { Data = data; Message = "操作成功"; }

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
    /// 错误响应，返回当前泛型类型的实例。
    /// 使用 <see langword="new"/> 隐藏基类方法以返回具体子类型，调用者应始终使用具体类型调用。
    /// 错误码必须为非0值（0 表示成功，与错误语义矛盾）
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">code 为 0 时抛出</exception>
    public static new ResponseResult<T> Error(int code, string message)
    {
        if (code == 0) throw new ArgumentOutOfRangeException(nameof(code), "错误码不能为 0（0 表示成功）");
        return new ResponseResult<T> { Code = code, Message = message };
    }
}
