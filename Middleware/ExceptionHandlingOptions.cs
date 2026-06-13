namespace Taipi.Core.Middleware;

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
    public int BadRequestCode { get; set; } = 2;

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
}
