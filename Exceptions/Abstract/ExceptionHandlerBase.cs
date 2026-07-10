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

    protected string GetFinalErrorMessage(T exception, HttpContext context, string defaultErrorMessage)
    {
        if (!IsDevelopment(context))

        {
            return defaultErrorMessage;
        }
        return _options.DetailedErrorMessageFactory?.Invoke(exception) ?? exception.Message;
    }

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