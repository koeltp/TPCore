# Taipi.Core

TaiPi Core Library for .NET 8.0+，封装通用响应模型、请求模型、中间件、异常处理和 LINQ 扩展方法。

## 项目结构

```
Taipi.Core/
├── RQRS/                    # 请求与响应模型
│   ├── StatusResponseResult.cs
│   ├── ResponseResult.cs
│   ├── PagerResponse.cs
│   ├── PagerResponseResult.cs
│   ├── SummaryPagerResponse.cs
│   ├── SummaryPagerResponseResult.cs
│   ├── Pager.cs             # 含 SortDirection 枚举、OrderByRQ、PagerEx
│   └── SearchPager.cs
├── Middleware/               # ASP.NET Core 中间件
│   ├── ExceptionHandlingMiddleware.cs
│   ├── RequestLoggingMiddleware.cs
│   ├── RequestLoggingOptions.cs
│   └── CorrelationIdMiddleware.cs
├── Exceptions/              # 异常类 + Handler 策略模式
│   ├── Abstract/
│   │   ├── IExceptionHandler.cs
│   │   ├── ExceptionHandlerBase.cs
│   │   └── ExceptionHandlerDelegateCache.cs  # 共享委托缓存
│   ├── TaipiCoreErrorCodes.cs     # 框架级错误码常量
│   ├── AppException.cs          # 业务异常基类 + ValidationException + ForbiddenException
│   ├── ExceptionHandlingOptions.cs
│   ├── AppExceptionHandler.cs
│   ├── ValidationHandler.cs
│   ├── ForbiddenHandler.cs
│   └── UnknownExceptionHandler.cs
├── Extensions/              # 服务注册扩展方法
│   ├── ExceptionHandlingExtensions.cs
│   ├── CorrelationIdExtensions.cs
│   ├── RequestLoggingExtensions.cs
│   ├── RateLimitingExtensions.cs  # 含 IPNetwork、RateLimitingOptions、RateLimitPolicies
│   └── SerilogExtensions.cs
├── Logging/                 # 日志增强
│   └── SensitiveDataEnricher.cs
├── Assertions/              # 断言扩展
│   └── AssertExtensions.cs
└── Linq/                    # LINQ 扩展
    ├── IQueryableEx.cs
    └── IEnumerableEx.cs
```

---

## 快速开始

在 `Program.cs` 中一键注册所有功能：

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Serilog 引导日志（捕获 Host 构建前的启动错误）
SerilogExtensions.CreateBootstrapLogger();
builder.Host.UseSerilogFromConfiguration();

// 2. 全局异常处理（可自定义错误码）
builder.Services.AddTaiPiExceptionHandling(options =>
{
    options.InvalidSortFieldErrorCode = TaipiCoreErrorCodes.InvalidSortField;  // 默认 1
    options.UnknownErrorCode = TaipiCoreErrorCodes.Unknown;                    // 默认 9999
});

// 3. 速率限制（配置受信代理以正确获取客户端 IP）
builder.Services.AddTaiPiRateLimiting(options =>
{
    options.GlobalPermitLimit = 100;
    options.LoginPermitLimit = 5;
    options.KnownProxies = [IPAddress.Parse("10.0.0.1"), IPAddress.Parse("10.0.0.2")];
    options.KnownNetworks = [new IPNetwork(IPAddress.Parse("172.16.0.0"), 16)];
});

var app = builder.Build();

// 中间件顺序很重要
app.UseCorrelationId();              // 链路追踪（最先注册）
app.UseTaiPiRequestLogging();        // 请求日志（外层，能观察到异常处理后的正确状态码）
app.UseTaiPiExceptionHandling();     // 全局异常处理（内层，靠近端点，先捕获异常）
app.UseRateLimiter();                // 速率限制

app.Run();
```

---

## 响应模型 (`Taipi.Core.RQRS`)

### 类图关系

```
StatusResponseResult                  (Code / Message / CorrelationId + 工厂方法)
├── ResponseResult<T>                 (+ Data)
├── PagerResponseResult<T>            (+ Data: PagerResponse<T>)
└── SummaryPagerResponseResult<T1, T2>  (+ Data: SummaryPagerResponse<T1, T2>)

PagerResponse<T>                      (Items / TotalCount / PageSize / PageIndex / PageCount)
└── SummaryPagerResponse<T1, T2>      (+ Summary)
```

> **设计约定**：`Code = 0` 表示成功，非 0 为业务错误码；所有业务异常统一返回 HTTP 200 + 业务错误码（SPA 友好）。

### StatusResponseResult

基类，包含业务状态码、消息和链路追踪标识，提供静态工厂方法。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Code` | `int` | 业务状态码，0=成功，非0=业务错误码 |
| `Message` | `string` | 响应消息描述 |
| `CorrelationId` | `string?` | 请求链路追踪标识（仅异常响应中由中间件赋值） |

```csharp
// 成功
StatusResponseResult.Success();
StatusResponseResult.Success("操作成功");

// 错误
StatusResponseResult.Error(1001, "用户名已存在");
```

### ResponseResult\<T\>

带数据的响应结果，成功时必须携带业务数据。

```csharp
// 构造函数
new ResponseResult<ClientDto>(client);

// 对象初始化器
new ResponseResult<ClientDto> { Data = client, Message = "查询成功" };

// 工厂方法（成功时必须传数据）
ResponseResult<ClientDto>.Success(client);
ResponseResult<ClientDto>.Success(client, "查询成功");
ResponseResult<ClientDto>.Error(1001, "客户已存在");
```

### PagerResponse\<T\>

纯分页数据容器，不包含响应状态。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Items` | `IEnumerable<T>` | 数据列表 |
| `TotalCount` | `int` | 总记录数（>= 0） |
| `PageSize` | `int` | 每页条数（>= 1，默认 10） |
| `PageIndex` | `int` | 当前页码（>= 1，默认 1） |
| `PageCount` | `int` | 总页数（只读计算） |

```csharp
new PagerResponse<ClientDto>
{
    Items = clients,
    PageIndex = 1,
    PageSize = 20,
    TotalCount = 100
};
```

### PagerResponseResult\<T\>

带状态的分页响应结果，成功时必须携带分页数据。

```csharp
// 构造函数（自动构建 PagerResponse<T>）
new PagerResponseResult<ClientDto>(items, pageIndex: 1, pageSize: 20, totalCount: 100);

// 工厂方法（成功时必须传分页数据）
PagerResponseResult<ClientDto>.Success(items, pageIndex: 1, pageSize: 20, totalCount: 100);
PagerResponseResult<ClientDto>.Success(items, pager, totalCount);
PagerResponseResult<ClientDto>.Error(2001, "查询失败");
```

### SummaryPagerResponse\<T1, T2\> / SummaryPagerResponseResult\<T1, T2\>

带汇总数据的分页模型，适用于需要合计/统计行的场景，成功时必须携带数据和汇总信息。

```csharp
// 构造函数
new SummaryPagerResponseResult<Order, OrderSummary>(
    items: orders,
    summary: new OrderSummary { TotalAmount = 99999 },
    pageIndex: 1,
    pageSize: 20,
    totalCount: 500
);

// 工厂方法（成功时必须传数据）
SummaryPagerResponseResult<Order, OrderSummary>.Success(
    orders, new OrderSummary { TotalAmount = 99999 }, 1, 20, 500);
```

---

## 请求模型 (`Taipi.Core.RQRS`)

### Pager

分页请求参数，PageSize 有最大值限制（默认 100）。

```csharp
public class Pager
{
    public static int MaxPageSize { get; set; } = 100;  // 最大每页记录数
    public int PageIndex { get; set; }   // >= 1，默认 1
    public int PageSize { get; set; }    // > 0，默认 10，最大 MaxPageSize
    public List<OrderByRQ>? OrderBy { get; set; }
}
```

### SearchPager\<T\>

带搜索条件的分页请求。

```csharp
public class SearchPager<T> : Pager
{
    public T Condition { get; set; } = default!;
}
```

```csharp
var request = new SearchPager<ClientFilter>
{
    PageIndex = 1,
    PageSize = 20,
    Condition = new ClientFilter { Name = "张三" }
};
```

### SortDirection 枚举

```csharp
public enum SortDirection
{
    Ascending = 0,   // 升序（ASC）
    Descending = 1   // 降序（DESC）
}
```

### OrderByRQ / PagerEx

排序条件，`Field` 属性在赋值时进行白名单校验以防止 SQL 注入（仅允许字母、数字、下划线、点号、方括号）。

```csharp
new OrderByRQ { Field = "CreateTime", Type = SortDirection.Descending }   // 降序

// 转换为 SQL ORDER BY 子句
orderByList.ToSql();  // "CreateTime DESC,Name ASC"

// 非法字段名会在赋值时抛出 ValidationException
// new OrderByRQ { Field = "1; DROP TABLE Users" };  // 抛出异常
```

---

## 中间件 (`Taipi.Core.Middleware`)

### 全局异常处理

统一捕获所有未处理异常，自动转换为标准 `StatusResponseResult` 响应格式。

#### 架构：策略模式 + 继承链回退

每种异常类型对应一个 `IExceptionHandler<T>` 实现，中间件通过 DI 动态解析：

```
异常抛出 → 沿继承链查找 Handler → 表达式树编译委托执行 → 返回标准化响应
```

**关键设计：**

| 特性 | 说明 |
|------|------|
| 继承链回退 | `TimeoutException` → 无专属 Handler → 沿链找到 `IExceptionHandler<Exception>` → `UnknownExceptionHandler` |
| 表达式树编译 | 首次遇到某异常类型后，Handle/GetLogLevel 方法编译为强类型委托，后续调用零反射开销 |
| 共享委托缓存 | `ExceptionHandlerDelegateCache` 供两个中间件共用，消除反射开销 |
| 日志级别委托 | 每个 Handler 自行决定 `GetLogLevel`，不再硬编码在中间件中 |

**内置 Handler 映射：**

| 异常类型 | Handler | HTTP 状态码 | 日志级别 | 说明 |
|----------|---------|------------|---------|------|
| `AppException` | `AppExceptionHandler` | 200 | Warning | 业务拒绝，前端判断 code |
| `ValidationException` | `ValidationHandler` | 200 | Information | 输入校验失败 |
| `ForbiddenException` | `ForbiddenHandler` | 200 | Information | 无权访问 |
| 其他 | `UnknownExceptionHandler` | 500 | Error | 系统异常，生产环境隐藏详情 |

#### 自定义 Handler

添加自定义异常和 Handler 只需两步：

```csharp
// 1. 定义异常类
public class PaymentFailedException(int code, string message) : AppException(code, message);

// 2. 定义 Handler（继承链回退：不注册也行，会回退到 AppExceptionHandler）
public class PaymentFailedHandler : ExceptionHandlerBase<PaymentFailedException>
{
    public PaymentFailedHandler(IWebHostEnvironment env, IOptions<ExceptionHandlingOptions> options) : base(env, options) { }

    public override (int StatusCode, StatusResponseResult Result) Handle(PaymentFailedException exception, HttpContext context)
        => (StatusCodes.Status200OK, StatusResponseResult.Error(exception.Code, exception.Message));

    public override LogLevel GetLogLevel(PaymentFailedException exception) => LogLevel.Warning;
}

// 3. 注册（可选，不注册则回退到 AppExceptionHandler）
builder.Services.AddScoped<IExceptionHandler<PaymentFailedException>, PaymentFailedHandler>();
```

> 不注册 Handler 时，`PaymentFailedException` 沿继承链找到 `AppExceptionHandler`，返回 HTTP 200 + 业务错误码。

#### 配置项

```csharp
builder.Services.AddTaiPiExceptionHandling(options =>
{
    options.InvalidSortFieldErrorCode = TaipiCoreErrorCodes.InvalidSortField;  // 默认 1
    options.UnknownErrorCode = TaipiCoreErrorCodes.Unknown;                    // 默认 9999
});

app.UseTaiPiExceptionHandling();
```

**异常响应自动携带 `CorrelationId`**，便于前端与日志关联排查。所有异常响应自动设置 `Cache-Control: no-store` 防止错误内容被缓存。

**未知异常**始终返回通用消息，详细异常仅记录在日志中（通过 CorrelationId 关联）。

---

## 异常类与 Handler (`Taipi.Core.Exceptions`)

### IExceptionHandler\<T\>

异常处理程序接口，每种异常类型对应一个实现。

```csharp
public interface IExceptionHandler<T> where T : Exception
{
    (int StatusCode, StatusResponseResult Result) Handle(T exception, HttpContext context);
    LogLevel GetLogLevel(T exception);
}
```

### ExceptionHandlerBase\<T\>

抽象基类，提供 `GetFinalErrorMessage`（生产环境脱敏）和 `IsDevelopment`（调试模式判断），默认 `GetLogLevel` 返回 `Warning`。

### TaipiCoreErrorCodes

框架级错误码常量及映射方法。

```csharp
public static class TaipiCoreErrorCodes
{
    public const int InvalidSortField = 1;   // 排序字段名非法
    public const int Unknown = 9999;         // 未知系统异常

    // 将框架错误码映射到 Options 配置值，业务自定义错误码原样返回
    public static int Mapper(int code, ExceptionHandlingOptions options);
}
```

> **编码规则**：框架级错误码使用 1-999 范围，业务自定义错误码使用 1000+ 范围（4 位数：模块编号 + 错误编号）。

### AppException

业务异常基类，中间件捕获后返回 HTTP 200 + 业务错误码，前端在 `.then` 中判断 code。

```csharp
// 业务规则拒绝
if (inventory < quantity)
    throw new AppException(100201, $"库存不足，当前仅剩 {inventory} 件");

// 在 Service 层使用
public async Task<ClientDto> CreateAsync(ClientRQ request)
{
    if (await _repo.ExistsAsync(request.Name))
        throw new AppException(2001, "客户名称已存在");
    // ...
}
```

### ValidationException / ForbiddenException

预定义的业务异常子类，统一返回 HTTP 200 + 业务错误码（SPA 友好）。

```csharp
throw new ValidationException(3001, "邮箱格式不正确");
throw new ForbiddenException(4001, "无权访问该资源");
```

### 断言扩展 (`Taipi.Core.Assertions`)

```csharp
using Taipi.Core.Assertions;

// 值为 true 时抛 AppException，为 false 时安全通过
(alreadyExists).ThrowIfTrue(1001, "记录已存在，不可重复创建");

// 值为 false 时抛 AppException，为 true 时安全通过
(isValid).ThrowIfFalse(1002, "数据校验失败");

// 值为 true 时执行操作，否则跳过
(hasUpdate).ExecuteIfTrue(() => ApplyChanges());
```

---

## 请求日志中间件

一行输出一个请求的完整信息，自动过滤低价值请求。

```
POST /api/client/search?name=test {"name":"test"} → 200 (45ms)
GET /api/client/1 → 404 (12ms)
POST /api/client → 500 (230ms)
```

**特性：**
- 自动记录请求方法、路径、查询参数、请求体、状态码、耗时
- 自动过滤 `/health`、`/swagger` 和静态文件请求
- 请求体超过 4KB 自动截断，避免日志膨胀
- 自动跳过文件上传请求（`multipart/form-data`、`application/octet-stream`）
- 异常请求的日志级别由 ExceptionHandler 决定（通过 HttpContext.Items 传递）
- 敏感字段正则预编译（`RegexOptions.Compiled`），避免每次请求重复编译

```csharp
app.UseTaiPiRequestLogging();
```

### CorrelationId 中间件

为每个请求生成或读取唯一链路标识，注入 Serilog 日志上下文。

**特性：**
- 优先读取请求头 `X-Correlation-Id`，不存在则自动生成
- 请求头值校验格式（仅允许字母、数字、连字符，长度 <= 64），非法时生成新 ID
- 响应头回写 `X-Correlation-Id`，便于前端追踪
- 通过 Serilog `LogContext` 注入，后续所有日志自动携带

```csharp
app.UseCorrelationId();
```

---

## 速率限制 (`Taipi.Core.Extensions`)

基于 ASP.NET Core 内置的 `RateLimiter`，提供按 IP 的滑动窗口限流。

**安全特性：**
- 仅在请求来自受信代理（`KnownProxies` / `KnownNetworks`）时才读取 `X-Forwarded-For` 头
- 默认不信任任何代理，直接使用 `RemoteIpAddress`，防止 IP 伪造绕过限流

**内置策略：**

| 策略 | 常量 | 默认限制 | 用途 |
|------|------|---------|------|
| 全局 | — | 100次/60秒 | 所有接口默认限流 |
| Token | `RateLimitPolicies.TokenEndpoint` | 10次/60秒 | 防暴力破解 |
| 登录 | `RateLimitPolicies.LoginEndpoint` | 5次/60秒 | 防暴力破解 |
| 外部登录 | `RateLimitPolicies.ExternalLoginEndpoint` | 5次/60秒 | 防 OAuth 滥用 |

```csharp
// 注册服务（可自定义各项参数）
builder.Services.AddTaiPiRateLimiting(options =>
{
    options.GlobalPermitLimit = 100;
    options.GlobalWindowSeconds = 60;
    options.LoginPermitLimit = 5;
    options.LoginWindowSeconds = 60;

    // 配置受信代理（仅来自这些 IP 的请求才读取 X-Forwarded-For）
    options.KnownProxies = [IPAddress.Parse("10.0.0.1")];
    options.KnownNetworks = [new IPNetwork(IPAddress.Parse("172.16.0.0"), 16)];
});

// 注册中间件
app.UseRateLimiter();

// 端点使用专属策略
app.MapPost("/api/login", Login).RequireRateLimiting(RateLimitPolicies.LoginEndpoint);
```

被限流时返回 `429 Too Many Requests` + `{"code":429,"message":"请求过于频繁，请稍后再试"}`。

---

## Serilog 集成 (`Taipi.Core.Extensions`)

```csharp
// 1. 引导日志（Program.cs 最顶部，捕获 Host 构建前的启动错误）
SerilogExtensions.CreateBootstrapLogger();

// 2. 配置 Host 使用 Serilog（从 appsettings.json 读取，已集成全局脱敏）
builder.Host.UseSerilogFromConfiguration();

// 3. 注册日志中间件
app.UseCorrelationId();      // 链路追踪（最先注册）
app.UseTaiPiRequestLogging();     // 请求日志（外层）
// app.UseTaiPiExceptionHandling(); // 异常处理应注册在请求日志之后（内层，靠近端点）
```

**appsettings.json 示例：**

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:HH:mm:ss} [{Level:u3}] [CorrelationId] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

---

## LINQ 扩展 (`Taipi.Core.Linq`)

### IQueryableEx

| 方法 | 说明 |
|------|------|
| `Page(pageIndex, pageSize)` | 数据库分页查询（边界自动修正：pageIndex < 1 → 1，pageSize <= 0 → 10） |
| `Page(Pager)` | 按 Pager 对象分页查询 |
| `WhereIf(condition, predicate)` | 条件过滤 |
| `OrderByIf(condition, keySelector)` | 条件排序 |

```csharp
var query = dbContext.Clients
    .WhereIf(!string.IsNullOrEmpty(name), c => c.Name.Contains(name))
    .OrderByIf(!string.IsNullOrEmpty(sortField), c => c.CreateTime)
    .Page(pageIndex: 1, pageSize: 20)
    .ToList();
```

### IEnumerableEx

| 方法 | 说明 |
|------|------|
| `Page(pageIndex, pageSize)` | 内存分页（边界自动修正） |
| `Page(Pager)` | 按 Pager 对象内存分页 |
| `WhereIf(condition, predicate)` | 条件过滤 |
| `ParallelForEachAsync(func)` | 并行异步遍历 |
| `ParallelForEachAsync(func, maxConcurrency)` | 指定最大并发数的并行异步遍历 |

---

## NuGet

```xml
<PackageReference Include="Taipi.Core" Version="1.3.15" />
```

### 本地打包

```bash
dotnet pack -c Release -o ./nupkg
```

生成到 `./nupkg/Taipi.Core.1.3.15.nupkg`，其他项目通过本地源引用：

```bash
dotnet add <项目> package Taipi.Core -s ./Taipi.Core/nupkg
```

### 推送到 NuGet 服务器

```bash
dotnet nuget push ./nupkg/Taipi.Core.1.3.15.nupkg --api-key <你的API密钥> --source https://api.nuget.org/v3/index.json
```

发布新版本时先改 `Taipi.Core.csproj` 里的 `<Version>`，再重新打包推送。
