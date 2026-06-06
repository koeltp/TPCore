# Taipi.Core

Taipi 基础类库，封装通用响应模型、请求模型和 LINQ 扩展方法。

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
└── Linq/               # LINQ 扩展
    ├── IQueryableEx.cs
    └── IEnumerableEx.cs
```

---

## 响应模型 (`Taipi.Core.RQRS`)

### 类图关系

```
StatusResponseResult                  (Code / Message / Timestamp + 工厂方法)
├── ResponseResult<T>                 (+ Data)
├── PagerResponseResult<T>            (+ Data: PagerResponse<T>)
└── SummaryPagerResponseResult<T1, T2>  (+ Data: SummaryPagerResponse<T1, T2>)

PagerResponse<T>                      (Items / TotalCount / PageSize / PageIndex / PageCount)
└── SummaryPagerResponse<T1, T2>  (+ Summary)
```

### StatusResponseResult

基类，包含状态码、消息和时间戳，提供静态工厂方法。

```csharp
// 成功
StatusResponseResult.Success();
StatusResponseResult.Success("操作成功");

// 错误
StatusResponseResult.Error(400, "参数错误");
StatusResponseResult.BadRequest();
StatusResponseResult.Unauthorized();
StatusResponseResult.Forbidden();
StatusResponseResult.NotFound();
StatusResponseResult.InternalError();
```

### ResponseResult\<T\>

带数据的响应结果。

```csharp
// 构造函数
new ResponseResult<ClientDto>(client);

// 对象初始化器
new ResponseResult<ClientDto> { Data = client, Message = "查询成功" };

// 工厂方法（返回 ResponseResult<T>）
ResponseResult<ClientDto>.Success("查询成功");
ResponseResult<ClientDto>.NotFound("客户未找到");
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

带状态的分页响应结果。

```csharp
// 构造函数（自动构建 PagerResponse<T>）
new PagerResponseResult<ClientDto>(items, pageIndex: 1, pageSize: 20, totalCount: 100);

// 工厂方法
PagerResponseResult<ClientDto>.Success("查询成功");
PagerResponseResult<ClientDto>.NotFound("暂无数据");
```

### SummaryPagerResponse\<T1, T2\> / SummaryPagerResponseResult\<T1, T2\>

带汇总数据的分页模型，适用于需要合计/统计行的场景。

```csharp
new SummaryPagerResponseResult<Order, OrderSummary>(
    items: orders,
    summary: new OrderSummary { TotalAmount = 99999 },
    pageIndex: 1,
    pageSize: 20,
    totalCount: 500
);
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

排序条件。

```csharp
new OrderByRQ { Field = "CreateTime", Type = 1 }   // 降序

// 转换为 SQL ORDER BY 子句
orderByList.ToSql();  // "CreateTime DESC,Name ASC"
```

---

## LINQ 扩展 (`Taipi.Core.Linq`)

### IQueryableEx

| 方法 | 说明 |
|------|------|
| `Page(pageIndex, pageSize)` | 分页查询 |
| `Page(Pager)` | 按 Pager 对象分页 |
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
<PackageReference Include="Taipi.Core" Version="1.0.0" />
```

### 本地打包

```bash
dotnet pack -c Release -o ./nupkg
```

生成到 `./nupkg/Taipi.Core.1.0.0.nupkg`，其他项目通过本地源引用：

```bash
dotnet add <项目> package Taipi.Core -s ./Taipi.Core/nupkg
```

### 推送到 NuGet 服务器

```bash
dotnet nuget push ./nupkg/Taipi.Core.1.0.0.nupkg --api-key <你的API密钥> --source https://api.nuget.org/v3/index.json
```

发布新版本时先改 `Taipi.Core.csproj` 里的 `<Version>`，再重新打包推送。