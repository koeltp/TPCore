namespace Taipi.Core.Exceptions;

/// <summary>
/// 全局异常处理中间件配置项，项目可覆盖框架异常的错误码和消息
/// </summary>
public class ExceptionHandlingOptions
{
    /// <summary>
    /// 未授权异常的错误码
    /// </summary>
    public int UnauthorizedCode { get; set; } = 1;

    /// <summary>
    /// 未授权异常的提示消息
    /// </summary>
    public string UnauthorizedMessage { get; set; } = "未授权";

    /// <summary>
    /// 参数错误异常的错误码
    /// </summary>
    public int ArgumentExceptionCode { get; set; } = 2;

    /// <summary>
    /// 参数错误异常的提示消息
    /// </summary>
    public string ArgumentExceptionMessage { get; set; } = "参数错误";

    /// <summary>
    /// 资源不存在异常的错误码
    /// </summary>
    public int NotFoundCode { get; set; } = 3;

    /// <summary>
    /// 资源不存在异常的提示消息
    /// </summary>
    public string NotFoundMessage { get; set; } = "资源不存在";

    /// <summary>
    /// 未知异常的错误码
    /// </summary>
    public int UnknownErrorCode { get; set; } = 9999;

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
