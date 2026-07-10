using Taipi.Core.RQRS;
using Microsoft.Extensions.Options;

using Taipi.Core.Exceptions.Abstract;

namespace Taipi.Core.Exceptions;

/// <summary>
/// 处理 <see cref="BadRequestException"/> 异常。
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
///   因为 <see cref="BadRequestException"/> 的 Message 是业务开发者
///   显式填写的面向最终用户的友好提示（如 "手机号格式不正确"），
///   不需要在生产环境进行脱敏或隐藏。
/// </para>
/// <para><b>与 <see cref="ArgumentExceptionHandler"/> 的区别：</b></para>
/// <para>
///   <see cref="ArgumentExceptionHandler"/> 会使用 <see cref="ExceptionHandlerBase{T}.GetFinalErrorMessage"/>
///   对消息进行脱敏，因为 <see cref="ArgumentException"/> 的消息可能包含内部参数名
///   （如 "Value cannot be null. (Parameter 'connectionString')"），
///   不适合直接返回给客户端,开启调试模式时会返回详细错误信息，但生产环境下会返回："请求参数无效" 。
/// </para>
/// </remarks>
public class BadRequestHandler : ExceptionHandlerBase<BadRequestException>
{
    public BadRequestHandler(IWebHostEnvironment env, IOptions<ExceptionHandlingOptions> options) : base(env, options) { }

    /// <summary>
    /// 处理 BadRequestException 异常
    /// </summary>
    /// <param name="exception">要处理的 BadRequestException 实例实例</param>
    /// <param name="context">当前 HTTP 上下文</param>
    /// <returns>包含状态码和状态响应结果的元组</returns>
    public override (int StatusCode, StatusResponseResult Result) Handle(BadRequestException exception, HttpContext context)
    {
        return (StatusCodes.Status400BadRequest, StatusResponseResult.Error(exception.Code, exception.Message));
    }
}