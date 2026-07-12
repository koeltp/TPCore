using Taipi.Core.RQRS;
using Microsoft.Extensions.Options;

using Taipi.Core.Exceptions.Abstract;

namespace Taipi.Core.Exceptions;

/// <summary>
/// 处理 <see cref="ValidationException"/> 校验异常。
/// </summary>
/// <remarks>
/// <para><b>处理策略：</b></para>
/// <list type="bullet">
///   <item><description>返回 HTTP 状态码 400 Bad Request</description></item>
///   <item><description>直接透传 Code 和 Message</description></item>
///   <item><description><b>不</b>使用 <see cref="ExceptionHandlerBase{T}.GetFinalErrorMessage"/> 方法</description></item>
/// </list>
/// <para><b>原因：</b></para>
/// <para>
///   因为 <see cref="ValidationException"/> 的 Message 是业务开发者
///   显式填写的面向最终用户的友好提示（如 "手机号格式不正确"），
///   不需要在生产环境进行脱敏或隐藏。
/// </para>
/// </remarks>
public class ValidationHandler : ExceptionHandlerBase<ValidationException>
{
    public ValidationHandler(IWebHostEnvironment env, IOptions<ExceptionHandlingOptions> options) : base(env, options) { }

    /// <summary>
    /// 处理 ValidationException 异常
    /// </summary>
    /// <param name="exception">要处理的 ValidationException 实例</param>
    /// <param name="context">当前 HTTP 上下文</param>
    /// <returns>包含状态码和状态响应结果的元组</returns>
    public override (int StatusCode, StatusResponseResult Result) Handle(ValidationException exception, HttpContext context)
    {
        var code = AppCodes.Mapper(exception.Code, _options);
        return (StatusCodes.Status400BadRequest, StatusResponseResult.Error(code, exception.Message));
    }
}
