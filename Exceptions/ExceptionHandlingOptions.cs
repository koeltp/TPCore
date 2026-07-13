namespace Taipi.Core.Exceptions;

/// <summary>
/// 全局异常处理中间件配置项
/// </summary>
public class ExceptionHandlingOptions
{
    /// <summary>
    /// 排序字段名非法的错误码，默认 <see cref="TaipiCoreErrorCodes.InvalidSortField"/>
    /// </summary>
    public int InvalidSortFieldErrorCode { get; set; } = TaipiCoreErrorCodes.InvalidSortField;

    /// <summary>
    /// 未知异常的错误码，默认 <see cref="TaipiCoreErrorCodes.Unknown"/>
    /// </summary>
    public int UnknownErrorCode { get; set; } = TaipiCoreErrorCodes.Unknown;

    /// <summary>
    /// 生产环境下的未知异常提示消息
    /// </summary>
    public string UnknownErrorMessage { get; set; } = "服务器内部错误";

    /// <summary>
    /// 非生产环境下的未知异常消息工厂，默认返回完整异常信息
    /// </summary>
    public Func<Exception, string> DetailedErrorMessageFactory { get; set; } = ex => ex.ToString();

    /// <summary>
    /// 是否记录异常日志（如果请求日志中间件已足够详细，可设为false避免重复）
    /// </summary>
    public bool LogException { get; set; } = true;

    /// <summary>
    /// 是否允许通过请求头 X-Debug 在生产环境返回详细错误
    /// </summary>
    public bool EnableDebugHeaderInProduction { get; set; } = false;
}
