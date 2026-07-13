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
}
