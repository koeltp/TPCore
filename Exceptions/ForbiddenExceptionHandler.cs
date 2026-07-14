using Taipi.Core.RQRS;
using Taipi.Core.Exceptions.Abstract;
using Microsoft.Extensions.Options;

namespace Taipi.Core.Exceptions;

/// <summary>
/// 处理 <see cref="ForbiddenException"/> 禁止访问异常。
/// </summary>
/// <remarks>
/// <para><b>处理策略：</b></para>
/// <list type="bullet">
///   <item><description>返回 HTTP 状态码 <b>200 OK</b>（SPA 统一通过业务错误码判断结果）</description></item>
///   <item><description>直接透传异常消息（权限消息始终面向用户，不经过环境判断）</description></item>
/// </list>
/// </remarks>
public class ForbiddenExceptionHandler : ExceptionHandlerBase<ForbiddenException>
{
    public ForbiddenExceptionHandler(IOptions<ExceptionHandlingOptions> options) : base(options) { }

    /// <summary>
    /// 处理 ForbiddenException 异常
    /// </summary>
    /// <param name="exception">要处理的 ForbiddenException 实例</param>
    /// <param name="context">当前 HTTP 上下文</param>
    /// <returns>包含状态码和状态响应结果的元组</returns>
    public override (int StatusCode, StatusResponseResult Result) Handle(ForbiddenException exception, HttpContext context)
    {
        var code = TaipiCoreErrorCodes.Mapper(exception.Code, _options);
        return (StatusCodes.Status200OK, StatusResponseResult.Error(code, exception.Message));
    }

    /// <summary>
    /// 权限不足属于正常业务分支，用 Information 避免产生过多告警
    /// </summary>
    public override LogLevel GetLogLevel(ForbiddenException exception) => LogLevel.Information;
}
