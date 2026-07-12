using Taipi.Core.RQRS;
using Microsoft.Extensions.Options;

namespace Taipi.Core.Exceptions.Abstract;
public abstract class ExceptionHandlerBase<T> : IExceptionHandler<T> where T : Exception
{
    protected readonly IWebHostEnvironment _env;
    protected readonly ExceptionHandlingOptions _options;

    protected ExceptionHandlerBase(IWebHostEnvironment env, IOptions<ExceptionHandlingOptions> options)
    {
        _env = env;
        _options = options.Value;
    }

    /// <summary>
    /// 获取最终的错误消息，根据当前环境和配置。
    /// </summary>
    /// <param name="exception">异常实例。</param>
    /// <param name="context">HTTP上下文。</param>
    /// <param name="defaultErrorMessage">默认错误消息。</param>
    /// <returns>最终的错误消息。</returns>
    protected string GetFinalErrorMessage(T exception, HttpContext context, string defaultErrorMessage)
    {
        if (IsDevelopment(context))
        {
            return _options.DetailedErrorMessageFactory?.Invoke(exception) ?? exception.Message;
        }
        return defaultErrorMessage;
    }

    /// <summary>
    /// 判断当前环境是否为开发环境或在生产环境中启用了调试头。
    /// </summary>
    /// <param name="context">HTTP上下文。</param>
    /// <returns>是否为开发环境或启用了调试头。</returns>
    protected bool IsDevelopment(HttpContext context)
    {
        return _env.IsDevelopment() || (_options.EnableDebugHeaderInProduction && context.Request.Headers.ContainsKey("X-Debug"));
    }

    public abstract (int StatusCode, StatusResponseResult Result) Handle(T exception, HttpContext context);

    /// <summary>
    /// 默认日志级别为 Warning，子类可按需覆盖
    /// </summary>
    public virtual LogLevel GetLogLevel(T exception) => LogLevel.Warning;
}