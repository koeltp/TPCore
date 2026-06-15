namespace Taipi.Core.Exceptions;

/// <summary>
/// 业务异常基类：用于业务层主动抛出的错误，中间件统一捕获后返回 HTTP 200 + 业务错误码
/// </summary>
public class AppException : Exception
{
    /// <summary>
    /// 业务错误码，0=成功，非0=错误
    /// </summary>
    public int Code { get; }

    ///业务异常基类：用于业务层主动抛出的错误，中间件统一捕获后返回 HTTP 200 + 业务错误码
    public AppException(int code, string message) : base(message)=>Code = code;

    /// <summary>
    /// 业务异常基类：用于业务层主动抛出的错误，中间件统一捕获后返回 HTTP 200 + 业务错误码
    /// </summary>
    /// <param name="code">业务错误码，0=成功，非0=错误</param>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public AppException(int code, string message, Exception innerException) : base(message, innerException)=>Code = code;
}

/// <summary>
/// 参数错误异常：用于参数校验失败时抛出的异常
/// </summary>
public class BadRequestException(int code, string message) : AppException(code, message);

/// <summary>
/// 禁止访问异常：用于用户没有权限访问资源时抛出的异常
/// </summary>
public class ForbiddenException(int code, string message) : AppException(code, message);
