using Taipi.Core.RQRS;
using Microsoft.Extensions.Options;

namespace Taipi.Core.Exceptions.Abstract;

/// <summary>
/// 异常处理程序抽象基类，提供配置访问和默认日志级别
/// </summary>
public abstract class ExceptionHandlerBase<T> : IExceptionHandler<T> where T : Exception
{
    protected readonly ExceptionHandlingOptions _options;

    protected ExceptionHandlerBase(IOptions<ExceptionHandlingOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// 处理异常，返回 HTTP 状态码和响应结果
    /// </summary>
    public abstract (int StatusCode, StatusResponseResult Result) Handle(T exception, HttpContext context);

    /// <summary>
    /// 默认日志级别为 Warning，子类可按需覆盖
    /// </summary>
    public virtual LogLevel GetLogLevel(T exception) => LogLevel.Warning;
}