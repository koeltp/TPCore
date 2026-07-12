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
    public AppException(int code, string message) : base(message) => Code = code;

    /// <summary>
    /// 业务异常基类：用于业务层主动抛出的错误，中间件统一捕获后返回 HTTP 200 + 业务错误码
    /// </summary>
    /// <param name="code">业务错误码，0=成功，非0=错误</param>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public AppException(int code, string message, Exception innerException) : base(message, innerException) => Code = code;
}

/// <summary>
/// 表示输入校验失败异常（HTTP 400）。
/// </summary>
/// <remarks>
/// <para><b>适用场景：</b></para>
/// <list type="bullet">
///   <item><description>用户输入格式错误（如邮箱、手机号格式不正确）</description></item>
///   <item><description>必填字段为空（如订单号不能为空）</description></item>
///   <item><description>业务参数超出有效范围（如分页页码不能小于1）</description></item>
/// </list>
/// <para><b>使用示例：</b></para>
/// <code>
/// if (string.IsNullOrWhiteSpace(email)) 
///     throw new ValidationException(3001, "邮箱地址不能为空");
/// </code>
/// </remarks>
public class ValidationException(int code, string message) : AppException(code, message);

/// <summary>
/// 禁止访问异常：用于用户没有权限访问资源时抛出的异常
/// </summary>
public class ForbiddenException(int code, string message) : AppException(code, message);
