using Taipi.Core.RQRS;
using Microsoft.Extensions.Options;
using Taipi.Core.Exceptions.Abstract;

namespace Taipi.Core.Exceptions;
/// <summary>
/// 处理所有未被其他 Handler 覆盖的未知异常。
/// </summary>
/// <remarks>
/// <para><b>处理策略：</b></para>
/// <list type="bullet">
///   <item><description>返回 HTTP 状态码 <b>500 Internal Server Error</b></description></item>
///   <item><description>使用 <see cref="ExceptionHandlerBase{T}.GetFinalErrorMessage"/> 决定最终消息内容</description></item>
///   <item><description>调试模式（开发环境或 X-Debug 头）→ 返回完整堆栈</description></item>
///   <item><description>生产模式 → 返回配置的通用消息（<see cref="ExceptionHandlingOptions.UnknownErrorMessage"/>）</description></item>
/// </list>
/// <para><b>适用场景：</b></para>
/// <list type="bullet">
///   <item><description>数据库连接超时（SqlException）</description></item>
///   <item><description>第三方 API 调用失败（HttpRequestException）</description></item>
///   <item><description>空引用异常（NullReferenceException）</description></item>
///   <item><description>任何未预料到的系统级错误</description></item>
/// </list>
/// <para><b>⚠️ 重要约定：</b></para>
/// <para>
///   此 Handler 是"最后的防线"，捕获所有未被其他 Handler 覆盖的异常。
///   永远不要主动抛出 <see cref="Exception"/>，而是让系统自然产生未知异常。
/// </para>
/// </remarks>
public class UnknownExceptionHandler : ExceptionHandlerBase<Exception>
{
    public UnknownExceptionHandler(IWebHostEnvironment environment, IOptions<ExceptionHandlingOptions> options) : base(environment, options) { }

    public override (int StatusCode, StatusResponseResult Result) Handle(Exception exception, HttpContext context)
    {
        var message = GetFinalErrorMessage(exception, context, _options.UnknownErrorMessage);
        return (StatusCodes.Status500InternalServerError, StatusResponseResult.Error(_options.UnknownErrorCode, message));
    }

    /// <summary>
    /// 未知异常必须以 Error 级别记录，触发告警
    /// </summary>
    public override LogLevel GetLogLevel(Exception exception) => LogLevel.Error;
}