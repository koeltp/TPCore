using Taipi.Core.RQRS;
using Taipi.Core.Exceptions.Abstract;
using Microsoft.Extensions.Options;
namespace Taipi.Core.Exceptions;


/// <summary>
/// 处理 <see cref="AppException"/> 业务规则拒绝异常。
/// </summary>
/// <remarks>
/// <para><b>处理策略：</b></para>
/// <list type="bullet">
///   <item><description>返回 HTTP 状态码 <b>200 OK</b></description></item>
///   <item><description>返回业务错误码 <see cref="AppException.Code"/> 和错误消息 <see cref="Exception.Message"/></description></item>
///   <item><description><b>不</b>使用 <see cref="ExceptionHandlerBase{T}.GetFinalErrorMessage"/> 方法（直接透传异常消息）</description></item>
/// </list>
/// 
/// <para><b>适用场景（典型业务拒绝）：</b></para>
/// <list type="bullet">
///   <item><description>库存不足（参数正确，但数量不够）</description></item>
///   <item><description>账户余额不足</description></item>
///   <item><description>订单状态不允许执行该操作（如已发货的订单不能取消）</description></item>
///   <item><description>活动未开始或已结束</description></item>
///   <item><description>重复请求/幂等性校验失败</description></item>
/// </list>
/// 
/// <para><b>与 <see cref="BadRequestException"/> 的区别：</b></para>
/// <para>
///   <see cref="BadRequestException"/> 表示"请求参数格式/值不正确"（如邮箱格式错误、必填字段为空），
///   属于 HTTP 400（客户端请求错误）。而 <see cref="AppException"/> 表示"请求参数没问题，
///   但业务规则不允许执行"，属于 HTTP 200（请求成功处理，但业务被拒绝）。
/// </para>
/// <para>
///   从 HTTP 语义角度：<see cref="BadRequestException"/> 意味着"客户端发来的东西是错的"；
///   <see cref="AppException"/> 意味着"服务器理解了请求，但业务规则说不让做"。
/// </para>
/// 
/// <para><b>与 <see cref="ArgumentExceptionHandler"/> 的区别：</b></para>
/// <para>
///   <see cref="ArgumentException"/> 是框架底层抛出的技术异常（如反序列化失败、类型转换失败），
///   其消息可能包含内部参数名，生产环境应脱敏。而 <see cref="AppException"/> 是业务层主动抛出的
///   友好异常，其 Message 是面向最终用户的业务提示（如 "库存不足，当前仅剩 5 件"），应直接返回。
/// </para>
/// 
/// <para><b>⚠️ 重要约定：</b></para>
/// <para>
///   业务代码中 <b>优先使用具体的子类</b>（如 <see cref="BadRequestException"/>、<see cref="ForbiddenException"/>），
///   仅在以上子类都不适用时，才直接抛出 <see cref="AppException"/>。
/// </para>
/// <para>
///   <b>禁止</b>用 <see cref="AppException"/> 包裹框架异常（如 SqlException、HttpRequestException），
///   这些系统级异常应让 <see cref="UnknownExceptionHandler"/> 处理并返回 500。
/// </para>
/// 
/// <para><b>使用示例：</b></para>
/// <code>
/// // ✅ 正确：在 Service 层校验业务规则
/// if (inventory &lt; quantity)
///     throw new AppException(100201, $"库存不足，当前仅剩 {inventory} 件");
/// 
/// if (order.Status == OrderStatus.Shipped)
///     throw new AppException(100202, "已发货的订单不能取消");
/// 
/// // ❌ 错误：不要用 AppException 包装技术异常
/// try { await _httpClient.GetAsync(url); } 
/// catch (HttpRequestException ex) { throw new AppException(100203, ex.Message); }
/// // 应该让 HttpRequestException 自然上抛，由 UnknownExceptionHandler 返回 500
/// </code>
/// </remarks>
public class AppExceptionHandler : ExceptionHandlerBase<AppException>
{
    public AppExceptionHandler(IWebHostEnvironment env, IOptions<ExceptionHandlingOptions> options) : base(env, options) { }
    /// <summary>
    /// 处理 AppException 异常
    /// </summary>
    /// <param name="exception">要处理的 AppException 实例实例</param>
    /// <param name="context">当前 HTTP 上下文</param>
    /// <returns>包含状态码和状态响应结果的元组</returns>
    public override (int StatusCode, StatusResponseResult Result) Handle(AppException exception, HttpContext context)
    {
        return (StatusCodes.Status200OK, StatusResponseResult.Error(exception.Code, exception.Message));
    }
}