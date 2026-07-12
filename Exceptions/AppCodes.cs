namespace Taipi.Core.Exceptions;

/// <summary>
/// 框架级错误码常量及映射方法。
/// <para>框架代码抛异常时使用这些常量，Handler 通过 <see cref="Mapper"/> 将其映射到
/// <see cref="ExceptionHandlingOptions"/> 中配置的实际值。业务自定义错误码直接透传。</para>
/// </summary>
public static class AppCodes
{
    /// <summary>
    /// 排序字段名非法（含 SQL 注入字符等）
    /// </summary>
    public const int InvalidSortField = 1;

    /// <summary>
    /// 未知系统异常
    /// </summary>
    public const int Unknown = 9999;

    /// <summary>
    /// 将框架错误码映射到 Options 配置值。业务自定义错误码原样返回。
    /// </summary>
    public static int Mapper(int code, ExceptionHandlingOptions options)
    {
        return code switch
        {
            InvalidSortField => options.InvalidSortFieldErrorCode,
            Unknown => options.UnknownErrorCode,
            _ => code,
        };
    }
}
