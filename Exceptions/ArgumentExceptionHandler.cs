using Taipi.Core.RQRS;
using Taipi.Core.Exceptions.Abstract;
using Taipi.Core.Middleware;
using Microsoft.Extensions.Options;

namespace Taipi.Core.Exceptions;

/// <summary>
/// 表示框架或底层工具层抛出的参数异常（HTTP 400）。
/// </summary>
/// <remarks>
/// <para><b>适用场景：</b></para>
/// <list type="bullet">
///   <item><description>JsonSerializer 反序列化失败</description></item>
///   <item><description>Guid.Parse 或 DateTime.Parse 解析失败</description></item>
///   <item><description>EF Core 或 Dapper 执行时参数校验失败</description></item>
/// </list>
/// <para><b>⚠️ 重要约定：</b></para>
/// <para>
///   业务代码中 <b>禁止直接抛出</b> <see cref="ArgumentException"/>。
///   所有业务校验失败应使用 <see cref="BadRequestException"/>。
///   本异常仅用于捕获框架底层抛出的实例，作为"防御性兜底"。
/// </para>
/// <para><b>生产环境行为：</b></para>
/// <para>
///   <see cref="ExceptionHandlingMiddleware"/> 会通过
///   <see cref="ExceptionHandlerBase{T}.GetFinalErrorMessage"/> 方法对 <see cref="ArgumentException"/>
///   进行"脱敏处理"，返回通用消息（如 "请求参数错误"），不会泄露内部参数名。
///   调试模式（开发环境或 X-Debug 头）下会返回完整堆栈信息。
/// </para>
/// </remarks>
public class ArgumentExceptionHandler : ExceptionHandlerBase<ArgumentException>
{
    public ArgumentExceptionHandler(IWebHostEnvironment env, IOptions<ExceptionHandlingOptions> options) : base(env, options) { }

    /// <summary>
    /// 处理 ArgumentException 异常
    /// </summary>
    /// <param name="exception">要处理的 ArgumentException 实例实例</param>
    /// <param name="context">当前 HTTP 上下文</param>
    /// <returns>包含状态码和状态响应结果的元组</returns>
    public override (int StatusCode, StatusResponseResult Result) Handle(ArgumentException exception, HttpContext context)
    {
        return (StatusCodes.Status400BadRequest, StatusResponseResult.Error(_options.ArgumentExceptionCode, GetFinalErrorMessage(exception, context, _options.ArgumentExceptionMessage)));
    }
}