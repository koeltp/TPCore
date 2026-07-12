using System.Linq.Expressions;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Taipi.Core.Exceptions;
using Taipi.Core.RQRS;
using Taipi.Core.Exceptions.Abstract;
using System.Collections.Concurrent;

namespace Taipi.Core.Middleware;

/// <summary>
/// 全局异常处理中间件：捕获管道中的异常，沿继承链查找 Handler 处理，
/// 编译委托缓存以优化性能，未匹配的异常回退到 UnknownExceptionHandler
/// </summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly ExceptionHandlingOptions _options;

    /// <summary>
    /// 已解析的 Handler 接口类型缓存：异常类型 → IExceptionHandler{T} 接口类型
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Type> _resolvedHandlerTypeCache = new();

    /// <summary>
    /// 编译后的 Handle 委托缓存：Handler 接口类型 → 编译委托
    /// </summary>
    private static readonly ConcurrentDictionary<Type, (Func<object, Exception, HttpContext, (int, StatusResponseResult)> Handle, Func<object, Exception, LogLevel> GetLogLevel)> _compiledDelegateCache = new();

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment,
        IOptions<ExceptionHandlingOptions> options)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(ex, "响应已开始发送，无法写入异常响应：{Message}", ex.Message);
                return;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // 1. 确定 HTTP 状态码、响应结果和日志级别
        var (httpStatusCode, result, logLevel) = MapExceptionToResponse(exception, context);

        // 2. 附加 CorrelationId
        result.CorrelationId = context.Items["CorrelationId"] as string;

        // 3. 将日志级别存入 HttpContext.Items，供下游中间件（如请求日志）使用
        context.Items["ExceptionLogLevel"] = logLevel;

        // 4. 记录异常日志（日志级别由 Handler 决定）
        if (_options.LogException)
        {
            var errorMessage = IsDebugMode(context)
                ? exception.ToString()
                : $"异常类型: {exception.GetType().Name}, 消息: {exception.Message}";

            _logger.Log(logLevel, exception, "请求处理异常: {Message}", errorMessage);
        }

        // 5. 写入响应
        context.Response.StatusCode = httpStatusCode;
        context.Response.ContentType = "application/json";

        // 禁用缓存，避免代理/CDN/浏览器缓存错误内容（特别是 GET 请求返回 200 错误时）
        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";

        await context.Response.WriteAsync(JsonSerializer.Serialize(result, _jsonOptions));
    }

    /// <summary>
    /// 根据异常类型映射到 HTTP 状态码、响应结果和日志级别。
    /// 沿继承链查找已注册的 Handler，未注册的类型自动回退到基类 Handler。
    /// </summary>
    private (int statusCode, StatusResponseResult result, LogLevel logLevel) MapExceptionToResponse(Exception exception, HttpContext context)
    {
        var exceptionType = exception.GetType();

        // 1. 查找已解析的 Handler 类型（首次异常后缓存，O(1)）
        if (!_resolvedHandlerTypeCache.TryGetValue(exceptionType, out var handlerInterfaceType))
        {
            handlerInterfaceType = ResolveHandlerType(exceptionType, context.RequestServices);
            _resolvedHandlerTypeCache.TryAdd(exceptionType, handlerInterfaceType);
        }

        // 2. 获取或创建编译后的委托（每个 Handler 类型只编译一次）
        var (handleFunc, getLogLevelFunc) = _compiledDelegateCache.GetOrAdd(handlerInterfaceType, CompileDelegates);

        // 3. 从 DI 解析 Handler 并执行
        using var scope = context.RequestServices.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService(handlerInterfaceType);
        var (statusCode, result) = handleFunc(handler, exception, context);
        var logLevel = getLogLevelFunc(handler, exception);

        return (statusCode, result, logLevel);
    }

    /// <summary>
    /// 沿异常继承链向上查找 DI 中已注册的 Handler，确保未注册的异常类型能回退到基类 Handler。
    /// 使用 IServiceProviderIsService 探测注册信息，避免在探测阶段创建实例。
    /// </summary>
    private static Type ResolveHandlerType(Type exceptionType, IServiceProvider serviceProvider)
    {
        var providerIsService = serviceProvider.GetService(typeof(IServiceProviderIsService)) as IServiceProviderIsService;
        var currentType = exceptionType;
        while (currentType != null && currentType != typeof(object))
        {
            var candidate = typeof(IExceptionHandler<>).MakeGenericType(currentType);
            // 优先使用 IServiceProviderIsService 判断注册（不创建实例），回退到 GetService
            if (providerIsService?.IsService(candidate) == true || serviceProvider.GetService(candidate) != null)
            {
                return candidate;
            }
            currentType = currentType.BaseType;
        }

        // 兜底：IExceptionHandler{Exception} 由 UnknownExceptionHandler 实现，始终注册
        return typeof(IExceptionHandler<Exception>);
    }

    /// <summary>
    /// 将 Handler 的 Handle 和 GetLogLevel 方法编译为强类型委托，消除反射调用开销
    /// </summary>
    private static (Func<object, Exception, HttpContext, (int, StatusResponseResult)> Handle, Func<object, Exception, LogLevel> GetLogLevel) CompileDelegates(Type handlerInterfaceType)
    {
        var handleMethod = handlerInterfaceType.GetMethod(nameof(IExceptionHandler<Exception>.Handle))
            ?? throw new InvalidOperationException($"Handler {handlerInterfaceType.Name} 未实现 Handle 方法");

        var getLogLevelMethod = handlerInterfaceType.GetMethod(nameof(IExceptionHandler<Exception>.GetLogLevel))
            ?? throw new InvalidOperationException($"Handler {handlerInterfaceType.Name} 未实现 GetLogLevel 方法");

        return (CompileHandleMethod(handleMethod), CompileGetLogLevelMethod(getLogLevelMethod));
    }

    /// <summary>
    /// 编译 Handle 方法为强类型委托：object → (int, StatusResponseResult)
    /// </summary>
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

    /// <summary>
    /// 编译 GetLogLevel 方法为强类型委托：object → LogLevel
    /// </summary>
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

    /// <summary>
    /// 判断是否为调试模式（开发环境或生产环境下的 X-Debug 头）
    /// </summary>
    private bool IsDebugMode(HttpContext context)
    {
        return _environment.IsDevelopment() || (_options.EnableDebugHeaderInProduction && context.Request.Headers.ContainsKey("X-Debug"));
    }
}
