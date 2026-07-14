using Taipi.Core.RQRS;
using Taipi.Core.Exceptions.Abstract;
using Microsoft.Extensions.Options;

namespace Taipi.Core.Exceptions;

/// <summary>
/// 处理 <see cref="OperationCanceledException"/> 客户端取消请求异常。
/// 客户端主动取消请求属于正常行为，不应记录为 Error 级别
/// </summary>
public class OperationCanceledHandler : ExceptionHandlerBase<OperationCanceledException>
{
    public OperationCanceledHandler(IOptions<ExceptionHandlingOptions> options) : base(options) { }

    /// <summary>
    /// 客户端已断开连接，返回 499 Client Closed Request（Nginx 惯例）。
    /// 实际上客户端已不再等待响应，此响应主要用于日志记录和管道完整性
    /// </summary>
    public override (int StatusCode, StatusResponseResult Result) Handle(OperationCanceledException exception, HttpContext context)
    {
        var code = TaipiCoreErrorCodes.Mapper(TaipiCoreErrorCodes.Unknown, _options);
        // 499 是 Nginx 对客户端主动关闭连接的定义，ASP.NET Core 无对应常量
        return (499, StatusResponseResult.Error(code, "客户端已取消请求"));
    }

    /// <summary>
    /// 客户端取消属于正常行为，使用 Debug 级别避免产生告警噪音
    /// </summary>
    public override LogLevel GetLogLevel(OperationCanceledException exception) => LogLevel.Debug;
}
