using Taipi.Core.Exceptions;

namespace Taipi.Core.Assertions;

/// <summary>
/// 布尔断言扩展方法，用于条件检查和异常抛出
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
    /// 值为 true 时执行操作，否则跳过
    /// </summary>
    /// <param name="value">布尔值</param>
    /// <param name="action">要执行的操作</param>
    public static void ExecuteIfTrue(this bool value, Action action)
    {
        if (value) action.Invoke();
    }
}
