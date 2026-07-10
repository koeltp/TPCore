using Taipi.Core.RQRS;
using Microsoft.Extensions.Options;

using Taipi.Core.Exceptions.Abstract;

namespace Taipi.Core.Exceptions;

/// <summary>
/// 401 错误异常处理程序，用于处理未授权访问的情况
/// </summary>
public class UnauthorizedAccessHandler : ExceptionHandlerBase<UnauthorizedAccessException>
{
    public UnauthorizedAccessHandler(IWebHostEnvironment env, IOptions<ExceptionHandlingOptions> options) : base(env, options) { }
    /// <summary>
    /// 处理 UnauthorizedAccessException 异常
    /// </summary>
    /// <param name="exception">要处理的 UnauthorizedAccessException 实例实例</param>
    /// <param name="context">当前 HTTP 上下文</param>
    /// <returns>包含状态码和状态响应结果的元组</returns>
    public override (int StatusCode, StatusResponseResult Result) Handle(UnauthorizedAccessException exception, HttpContext context)
    {
        return (StatusCodes.Status401Unauthorized, StatusResponseResult.Error(_options.UnauthorizedCode, GetFinalErrorMessage(exception, context, _options.UnauthorizedMessage)));
    }
}
