using Taipi.Core.RQRS;
using Microsoft.Extensions.Options;
using Taipi.Core.Exceptions.Abstract;

namespace Taipi.Core.Exceptions;

/// <summary>
/// 404 错误异常处理程序，用于处理键未找到的情况
/// </summary>
public class KeyNotFoundHandler : ExceptionHandlerBase<KeyNotFoundException>
{
    public KeyNotFoundHandler(IWebHostEnvironment env, IOptions<ExceptionHandlingOptions> options) : base(env, options) { }
    /// <summary>
    /// 处理 KeyNotFoundException 异常
    /// </summary>
    /// <param name="exception">要处理的 KeyNotFoundException 实例实例</param>
    /// <param name="context">当前 HTTP 上下文</param>
    /// <returns>包含状态码和状态响应结果的元组</returns>
    public override (int StatusCode, StatusResponseResult Result) Handle(KeyNotFoundException exception, HttpContext context)
    {
        return (StatusCodes.Status404NotFound, StatusResponseResult.Error(_options.NotFoundCode, GetFinalErrorMessage(exception, context, _options.NotFoundMessage)));
    }

    /// <summary>
    /// 资源不存在属于常见业务场景，用 Information 避免产生过多告警
    /// </summary>
    public override LogLevel GetLogLevel(KeyNotFoundException exception) => LogLevel.Information;
}
