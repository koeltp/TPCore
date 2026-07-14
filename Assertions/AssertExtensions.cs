using Taipi.Core.Exceptions;

namespace Taipi.Core.Assertions;

/// <summary>
/// 断言扩展方法，用于条件检查和异常抛出
/// </summary>
public static class AssertExtensions
{
    /// <summary>
    /// 值为 true 时抛出 AppException，为 false 时安全通过
    /// </summary>
    /// <param name="value">布尔值</param>
    /// <param name="errorCode">错误码</param>
    /// <param name="errorMsg">错误信息</param>
    public static void ThrowIfTrue(this bool value, int errorCode, string errorMsg)
    {
        ThrowIfTrue(value, new AppException(errorCode, errorMsg));
    }

    /// <summary>
    /// 值为 true 时抛出指定异常，为 false 时安全通过
    /// </summary>
    /// <param name="value">布尔值</param>
    /// <param name="exception">要抛出的异常</param>
    public static void ThrowIfTrue(this bool value, AppException exception)
    {
        if (value) throw exception;
    }

    /// <summary>
    /// 值为 false 时抛出 AppException，为 true 时安全通过
    /// </summary>
    /// <param name="value">布尔值</param>
    /// <param name="errorCode">错误码</param>
    /// <param name="errorMsg">错误信息</param>
    public static void ThrowIfFalse(this bool value, int errorCode, string errorMsg)
    {
        ThrowIfTrue(!value, new AppException(errorCode, errorMsg));
    }

    /// <summary>
    /// 值为 false 时抛出指定异常，为 true 时安全通过
    /// </summary>
    /// <param name="value">布尔值</param>
    /// <param name="exception">要抛出的异常</param>
    public static void ThrowIfFalse(this bool value, AppException exception)
    {
        ThrowIfTrue(!value, exception);
    }

    /// <summary>
    /// 值为 null 时抛出 AppException，不为 null 时安全通过
    /// </summary>
    /// <typeparam name="T">引用类型</typeparam>
    /// <param name="value">要检查的对象</param>
    /// <param name="errorCode">错误码</param>
    /// <param name="errorMsg">错误信息</param>
    public static void ThrowIfNull<T>(this T? value, int errorCode, string errorMsg) where T : class
    {
        ThrowIfNull(value, new AppException(errorCode, errorMsg));
    }

    public static void ThrowIfNull<T>(this T? value, AppException exception) where T : class
    {
        if (value is null) throw exception;
    }

    public static void ThrowIfNotNull<T>(this T? value, int errorCode, string errorMsg) where T : class
    {
        ThrowIfNotNull(value, new AppException(errorCode, errorMsg));
    }

    public static void ThrowIfNotNull<T>(this T? value, AppException exception) where T : class
    {
        if (value is not null) throw exception;
    }

    /// <summary>
    /// 字符串为 null、空或仅含空白字符时抛出 AppException，否则安全通过
    /// </summary>
    /// <param name="value">要检查的字符串</param>
    /// <param name="errorCode">错误码</param>
    /// <param name="errorMsg">错误信息</param>
    public static void ThrowIfNullOrWhiteSpace(this string? value, int errorCode, string errorMsg)
    {
        ThrowIfNullOrWhiteSpace(value, new AppException(errorCode, errorMsg));
    }

    public static void ThrowIfNullOrWhiteSpace(this string? value, AppException exception)
    {
        if (string.IsNullOrWhiteSpace(value)) throw exception;
    }
    /// <summary>
    /// 值为 true 时执行操作，否则跳过
    /// </summary>
    /// <param name="value">布尔值</param>
    /// <param name="action">要执行的操作</param>
    public static void ExecuteIfTrue(this bool value, Action action)
    {
        if (value) action.Invoke();
    }
}
