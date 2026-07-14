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
///   <item><description>返回 HTTP 状态码 <b>200 OK</b>（SPA 统一通过业务错误码判断结果）</description></item>
///   <item><description>直接透传异常消息（校验消息始终面向用户，不经过环境判断）</description></item>
/// </list>
/// <para><b>原因：</b></para>
/// <para>
///   因为 <see cref="ValidationException"/> 的 Message 是业务开发者
///   显式填写的面向最终用户的友好提示（如 "手机号格式不正确"），
///   不需要在生产环境进行脱敏或隐藏。
/// </para>
/// </remarks>
public class ValidationExceptionHandler : ExceptionHandlerBase<ValidationException>
{
    public ValidationExceptionHandler(IOptions<ExceptionHandlingOptions> options) : base(options) { }

    /// <summary>
    /// 处理 ValidationException 异常
    /// </summary>
    /// <param name="exception">要处理的 ValidationException 实例</param>
    /// <param name="context">当前 HTTP 上下文</param>
    /// <returns>包含状态码和状态响应结果的元组</returns>
    public override (int StatusCode, StatusResponseResult Result) Handle(ValidationException exception, HttpContext context)
    {
        var code = TaipiCoreErrorCodes.Mapper(exception.Code, _options);
        return (StatusCodes.Status200OK, StatusResponseResult.Error(code, exception.Message));
    }

    /// <summary>
    /// 参数校验失败属于正常业务分支，用 Information 避免产生过多告警
    /// </summary>
    public override LogLevel GetLogLevel(ValidationException exception) => LogLevel.Information;
}
