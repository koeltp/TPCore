using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Taipi.Core.RQRS;

namespace Taipi.Core.Exceptions.Abstract;

/// <summary>
/// 异常 Handler 委托缓存，供 ExceptionHandlingMiddleware 和 RequestLoggingMiddleware 共用。
/// 首次遇到某异常类型后，Handle/GetLogLevel 方法编译为强类型委托，后续调用零反射开销。
/// </summary>
internal static class ExceptionHandlerDelegateCache
{
    /// <summary>
    /// 已解析的 Handler 接口类型缓存：异常类型 → IExceptionHandler{T} 接口类型
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Type> _resolvedHandlerTypeCache = new();

    /// <summary>
    /// 编译后的委托缓存：Handler 接口类型 → (Handle 委托, GetLogLevel 委托)
    /// </summary>
    private static readonly ConcurrentDictionary<Type, (Func<object, Exception, HttpContext, (int, StatusResponseResult)> Handle, Func<object, Exception, LogLevel> GetLogLevel)> _compiledDelegateCache = new();

    /// <summary>
    /// 解析异常类型对应的 Handler 接口类型，沿继承链回退
    /// </summary>
    public static Type ResolveHandlerType(Type exceptionType, IServiceProvider serviceProvider)
    {
        return _resolvedHandlerTypeCache.GetOrAdd(exceptionType, type =>
        {
            var providerIsService = serviceProvider.GetService(typeof(IServiceProviderIsService)) as IServiceProviderIsService;
            var currentType = type;
            while (currentType != null && currentType != typeof(object))
            {
                var candidate = typeof(IExceptionHandler<>).MakeGenericType(currentType);
                if (providerIsService?.IsService(candidate) == true || serviceProvider.GetService(candidate) != null)
                    return candidate;
                currentType = currentType.BaseType;
            }
            return typeof(IExceptionHandler<Exception>);
        });
    }

    /// <summary>
    /// 获取或编译 Handle 和 GetLogLevel 委托
    /// </summary>
    public static (Func<object, Exception, HttpContext, (int, StatusResponseResult)> Handle, Func<object, Exception, LogLevel> GetLogLevel) GetOrCompileDelegates(Type handlerInterfaceType)
    {
        return _compiledDelegateCache.GetOrAdd(handlerInterfaceType, CompileDelegates);
    }

    /// <summary>
    /// 通过异常类型直接获取日志级别（供 RequestLoggingMiddleware 使用）
    /// </summary>
    public static LogLevel GetLogLevel(Exception exception, IServiceProvider serviceProvider)
    {
        var handlerInterfaceType = ResolveHandlerType(exception.GetType(), serviceProvider);
        var (_, getLogLevelFunc) = GetOrCompileDelegates(handlerInterfaceType);

        using var scope = serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService(handlerInterfaceType);
        return getLogLevelFunc(handler, exception);
    }

    private static (Func<object, Exception, HttpContext, (int, StatusResponseResult)> Handle, Func<object, Exception, LogLevel> GetLogLevel) CompileDelegates(Type handlerInterfaceType)
    {
        var handleMethod = handlerInterfaceType.GetMethod(nameof(IExceptionHandler<Exception>.Handle))
            ?? throw new InvalidOperationException($"Handler {handlerInterfaceType.Name} 未实现 Handle 方法");

        var getLogLevelMethod = handlerInterfaceType.GetMethod(nameof(IExceptionHandler<Exception>.GetLogLevel))
            ?? throw new InvalidOperationException($"Handler {handlerInterfaceType.Name} 未实现 GetLogLevel 方法");

        return (CompileHandleMethod(handleMethod), CompileGetLogLevelMethod(getLogLevelMethod));
    }

    private static Func<object, Exception, HttpContext, (int, StatusResponseResult)> CompileHandleMethod(MethodInfo handleMethod)
    {
        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var exParam = Expression.Parameter(typeof(Exception), "ex");
        var ctxParam = Expression.Parameter(typeof(HttpContext), "ctx");

        var call = Expression.Call(
            Expression.Convert(handlerParam, handleMethod.DeclaringType!),
            handleMethod,
            Expression.Convert(exParam, handleMethod.GetParameters()[0].ParameterType),
            ctxParam
        );

        return Expression.Lambda<Func<object, Exception, HttpContext, (int, StatusResponseResult)>>(
            call, handlerParam, exParam, ctxParam).Compile();
    }

    private static Func<object, Exception, LogLevel> CompileGetLogLevelMethod(MethodInfo getLogLevelMethod)
    {
        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var exParam = Expression.Parameter(typeof(Exception), "ex");

        var call = Expression.Call(
            Expression.Convert(handlerParam, getLogLevelMethod.DeclaringType!),
            getLogLevelMethod,
            Expression.Convert(exParam, getLogLevelMethod.GetParameters()[0].ParameterType)
        );

        return Expression.Lambda<Func<object, Exception, LogLevel>>(
            call, handlerParam, exParam).Compile();
    }
}
