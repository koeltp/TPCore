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

    public AppException(int code, string message) : base(message)
    {
        Code = code;
    }

    public AppException(int code, string message, Exception innerException) : base(message, innerException)
    {
        Code = code;
    }
}

/// <summary>
/// 参数错误异常
/// </summary>
public class BadRequestException(int code, string message) : AppException(code, message);

/// <summary>
/// 禁止访问异常
/// </summary>
public class ForbiddenException(int code, string message) : AppException(code, message);
