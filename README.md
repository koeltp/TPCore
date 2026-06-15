# Taipi.Core

TaiPi Core Library for .NET 8.0+，封装通用响应模型、请求模型、中间件、异常处理和 LINQ 扩展方法。

## 项目结构

```
Taipi.Core/
├── RQRS/               # 请求与响应模型
│   ├── StatusResponseResult.cs
│   ├── ResponseResult.cs
│   ├── PagerResponse.cs
│   ├── PagerResponseResult.cs
│   ├── SummaryPagerResponse.cs
│   ├── SummaryPagerResponseResult.cs
│   ├── Pager.cs
│   └── SearchPager.cs
├── Middleware/          # ASP.NET Core 中间件
│   ├── ExceptionHandlingMiddleware.cs
│   ├── ExceptionHandlingOptions.cs
│   ├── RequestLoggingMiddleware.cs
│   └── CorrelationIdMiddleware.cs
├── Extensions/         # 服务注册扩展方法
│   ├── TaiPiCoreExtensions.cs
│   ├── SerilogExtensions.cs
│   └── RateLimitingExtensions.cs
├── Exceptions/         # 自定义异常类
│   └── AppException.cs
└── Linq/               # LINQ 扩展
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

// 2. 全局异常处理（可自定义错误码和消息）
builder.Services.AddTaiPiExceptionHandling(options =>
{
    options.UnauthorizedCode = 401;
    options.UnauthorizedMessage = "登录已过期";
    options.NotFoundCode = 404;
    options.NotFoundMessage = "数据不存在";
});

// 3. 速率限制
builder.Services.AddTaiPiRateLimiting(options =>
{
    options.GlobalPermitLimit = 100;
    options.LoginPermitLimit = 5;
});

var app = builder.Build();

// 中间件顺序很重要
app.UseCorrelationId();              // 链路追踪（最先注册）
app.UseTaiPiExceptionHandling();     // 全局异常处理
app.UseRequestLogging();             // 请求日志
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

> **设计约定**：`Code = 0` 表示成功，非 0 为业务错误码；HTTP 状态码仅用于框架级错误（4xx/5xx）。

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

分页请求参数。

```csharp
public class Pager
{
    public int PageIndex { get; set; }   // >= 1，默认 1
    public int PageSize { get; set; }    // > 0，默认 10
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

### OrderByRQ / PagerEx

排序条件，`Field` 属性在赋值时进行白名单校验以防止 SQL 注入（仅允许字母、数字、下划线、点号、方括号）。

```csharp
new OrderByRQ { Field = "CreateTime", Type = 1 }   // 降序

// 转换为 SQL ORDER BY 子句
orderByList.ToSql();  // "CreateTime DESC,Name ASC"

// 非法字段名会在赋值时抛出 ArgumentException
// new OrderByRQ { Field = "1; DROP TABLE Users" };  // 抛出异常
```

---

## 中间件 (`Taipi.Core.Middleware`)

### 全局异常处理

统一捕获所有未处理异常，自动转换为标准 `StatusResponseResult` 响应格式。

**异常映射规则：**

| 异常类型 | HTTP 状态码 | 业务码 | 说明 |
|----------|------------|--------|------|
| `AppException` | 200 | 自定义 | 业务异常，前端在 `.then` 中判断 code |
| `UnauthorizedAccessException` | 401 | 可配置 | 框架异常，前端在拦截器中处理 |
| `ArgumentException` | 400 | 可配置 | 参数校验失败 |
| `KeyNotFoundException` | 404 | 可配置 | 资源不存在 |
| 其他 | 500 | 可配置 | 未知异常，生产环境隐藏详情 |

```csharp
// 注册服务（可自定义错误码和消息）
builder.Services.AddTaiPiExceptionHandling(options =>
{
    options.UnauthorizedCode = 401;
    options.UnauthorizedMessage = "登录已过期";
    options.NotFoundCode = 404;
    options.NotFoundMessage = "数据不存在";
    options.UnknownErrorCode = 9999;
    options.UnknownErrorMessage = "服务器内部错误";
    // 非生产环境返回完整异常信息
    options.DetailedErrorMessageFactory = ex => ex.ToString();
});

// 注册中间件
app.UseTaiPiExceptionHandling();
```

**异常响应自动携带 `CorrelationId`**，便于前端与日志关联排查。

### 请求日志中间件

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
- 状态码 >= 500 为 Error，>= 400 为 Warning，其余为 Information

```csharp
app.UseRequestLogging();
```

### CorrelationId 中间件

为每个请求生成或读取唯一链路标识，注入 Serilog 日志上下文。

**特性：**
- 优先读取请求头 `X-Correlation-Id`，不存在则自动生成
- 响应头回写 `X-Correlation-Id`，便于前端追踪
- 通过 Serilog `LogContext` 注入，后续所有日志自动携带

```csharp
app.UseCorrelationId();
```

---

## 异常类 (`Taipi.Core.Exceptions`)

### AppException

业务异常基类，中间件捕获后返回 HTTP 200 + 业务错误码，前端在 `.then` 中判断 code。

```csharp
// 抛出业务异常
throw new AppException(1001, "用户名已存在");

// 在服务层使用
public async Task<ClientDto> CreateAsync(ClientRQ request)
{
    if (await _repo.ExistsAsync(request.Name))
        throw new AppException(2001, "客户名称已存在");
    // ...
}
```

### BadRequestException / ForbiddenException

预定义的业务异常快捷类。

```csharp
throw new BadRequestException(3001, "邮箱格式不正确");
throw new ForbiddenException(4001, "无权访问该资源");
```

---

## 速率限制 (`Taipi.Core.Extensions`)

基于 ASP.NET Core 内置的 `RateLimiter`，提供按 IP 的滑动窗口限流。

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

// 2. 配置 Host 使用 Serilog（从 appsettings.json 读取）
builder.Host.UseSerilogFromConfiguration();

// 3. 注册日志中间件
app.UseCorrelationId();      // 链路追踪（最先注册）
app.UseRequestLogging();     // 请求日志
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
| `Page(pageIndex, pageSize)` | 数据库分页查询 |
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
| `Page(pageIndex, pageSize)` | 内存分页 |
| `Page(Pager)` | 按 Pager 对象内存分页 |
| `WhereIf(condition, predicate)` | 条件过滤 |
| `ParallelForEachAsync(func)` | 并行异步遍历 |
| `ParallelForEachAsync(func, dop)` | 指定并发的并行异步遍历 |

---

## NuGet

```xml
<PackageReference Include="Taipi.Core" Version="1.3.1" />
```

### 本地打包

```bash
dotnet pack -c Release -o ./nupkg
```

生成到 `./nupkg/Taipi.Core.1.3.1.nupkg`，其他项目通过本地源引用：

```bash
dotnet add <项目> package Taipi.Core -s ./Taipi.Core/nupkg
```

### 推送到 NuGet 服务器

```bash
dotnet nuget push ./nupkg/Taipi.Core.1.3.1.nupkg --api-key <你的API密钥> --source https://api.nuget.org/v3/index.json
```

发布新版本时先改 `Taipi.Core.csproj` 里的 `<Version>`，再重新打包推送。
