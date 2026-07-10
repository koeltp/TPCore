using Taipi.Core.RQRS;
using Taipi.Core.Exceptions.Abstract;
using Microsoft.Extensions.Options;

namespace Taipi.Core.Exceptions;
/// <summary>
/// 403 错误异常处理程序，用于处理访问被拒绝的情况
/// </summary>
public class ForbiddenHandler : ExceptionHandlerBase<ForbiddenException>
{
    public ForbiddenHandler(IWebHostEnvironment env, IOptions<ExceptionHandlingOptions> options) : base(env, options) { }
    /// <summary>
    /// 处理 ForbiddenException 异常
    /// </summary>
    /// <param name="exception">要处理的 ForbiddenException 实例实例</param>
    /// <param name="context">当前 HTTP 上下文</param>
    /// <returns>包含状态码和状态响应结果的元组</returns>
    public override (int StatusCode, StatusResponseResult Result) Handle(ForbiddenException exception, HttpContext context)
    {
        return (StatusCodes.Status403Forbidden, StatusResponseResult.Error(exception.Code, exception.Message));
    }

    /// <summary>
    /// 权限不足属于正常业务分支，用 Information 避免产生过多告警
    /// </summary>
    public override LogLevel GetLogLevel(ForbiddenException exception) => LogLevel.Information;
}