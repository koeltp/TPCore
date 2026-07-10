using Taipi.Core.RQRS;

namespace Taipi.Core.Exceptions.Abstract;
/// <summary>
/// 异常处理程序接口，用于定义如何处理指定类型的异常
/// </summary>
/// <typeparam name="T">要处理的异常类型</typeparam>
public interface IExceptionHandler<T> where T : Exception
{
    /// <summary>
    /// 处理指定类型的异常
    /// </summary>
    /// <param name="exception">要处理的异常实例</param>
    /// <param name="context">当前 HTTP 上下文</param>
    /// <returns>包含状态码和状态响应结果的元组</returns>
    (int StatusCode, StatusResponseResult Result) Handle(T exception, HttpContext context);
}
