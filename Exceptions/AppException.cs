namespace Taipi.Core.Exceptions;

/// <summary>
/// 业务异常基类：用于业务层主动抛出的错误，中间件统一捕获后返回 HTTP 200 + 业务错误码
/// </summary>
/// <remarks>
/// <para><b>错误码约定：</b></para>
/// <list type="bullet">
///   <item><description>框架级错误码：1-999 范围，定义在 <see cref="AppCodes"/></description></item>
///   <item><description>业务自定义错误码：1000+ 范围（4 位数，模块编号 + 错误编号）</description></item>
/// </list>
/// </remarks>
public class AppException : Exception
{
    /// <summary>
    /// 业务错误码，0=成功，非0=错误
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// 创建业务异常（不含内部异常）
    /// </summary>
    /// <param name="code">业务错误码，0=成功，非0=错误</param>
    /// <param name="message">面向用户的异常消息</param>
    public AppException(int code, string message) : base(message) => Code = code;

    /// <summary>
    /// 创建业务异常（包含内部异常）
    /// </summary>
    /// <param name="code">业务错误码，0=成功，非0=错误</param>
    /// <param name="message">面向用户的异常消息</param>
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
public class ValidationException : AppException
{
    /// <summary>
    /// 创建校验异常
    /// </summary>
    public ValidationException(int code, string message) : base(code, message) { }

    /// <summary>
    /// 创建校验异常（包含内部异常）
    /// </summary>
    public ValidationException(int code, string message, Exception innerException) : base(code, message, innerException) { }
}

/// <summary>
/// 表示禁止访问异常（HTTP 403）。
/// </summary>
/// <remarks>
/// <para><b>适用场景：</b></para>
/// <list type="bullet">
///   <item><description>角色权限不足（如普通用户访问管理接口）</description></item>
///   <item><description>资源所有权校验失败（如修改他人订单）</description></item>
///   <item><description>功能未授权（如免费用户访问付费功能）</description></item>
/// </list>
/// <para><b>与 HTTP 401 的区别：</b>401 表示未认证（Token 缺失/过期），403 表示已认证但无权访问。</para>
/// <para><b>使用示例：</b></para>
/// <code>
/// if (!user.HasRole(Role.Admin))
///     throw new ForbiddenException(AppCodes.Forbidden, "仅管理员可执行此操作");
/// </code>
/// </remarks>
public class ForbiddenException : AppException
{
    /// <summary>
    /// 创建禁止访问异常
    /// </summary>
    public ForbiddenException(int code, string message) : base(code, message) { }

    /// <summary>
    /// 创建禁止访问异常（包含内部异常）
    /// </summary>
    public ForbiddenException(int code, string message, Exception innerException) : base(code, message, innerException) { }
}
