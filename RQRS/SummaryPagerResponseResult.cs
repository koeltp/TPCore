namespace Taipi.Core.RQRS;

/// <summary>
/// 带汇总数据的分页响应结果类，在分页基础上额外携带汇总信息（如合计、统计等）
/// </summary>
/// <typeparam name="T1">分页列表项的数据类型</typeparam>
/// <typeparam name="T2">汇总数据的类型</typeparam>
public class SummaryPagerResponseResult<T1, T2> : StatusResponseResult
{
    /// <summary>带汇总的分页响应数据</summary>
    public SummaryPagerResponse<T1, T2>? Data { get; set; }

    /// <summary>无参构造函数，用于反序列化或后续手动赋值场景</summary>
    public SummaryPagerResponseResult() { }

    /// <summary>
    /// 通过分页数据和汇总信息构造响应结果，Code 默认为 0（成功），Message 默认为"操作成功"
    /// </summary>
    /// <param name="items">当前页的数据列表</param>
    /// <param name="summary">汇总数据（如合计值、统计信息等）</param>
    /// <param name="pageIndex">当前页码（从1开始）</param>
    /// <param name="pageSize">每页记录数</param>
    /// <param name="totalCount">总记录数</param>
    public SummaryPagerResponseResult(IEnumerable<T1> items, T2 summary, int pageIndex, int pageSize, int totalCount)
    {
        Message = "操作成功";
        Data = new SummaryPagerResponse<T1, T2>
        {
            Items = items,
            Summary = summary,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
    /// <summary>
    /// 通过分页数据和汇总信息构造响应结果，Code 默认为 0（成功）
    /// </summary>
    /// <param name="items">当前页的数据列表</param>
    /// <param name="summary">汇总数据（如合计值、统计信息等）</param>
    /// <param name="pager">分页请求参数（包含页码和每页大小）</param>
    /// <param name="totalCount">总记录数</param>
    public SummaryPagerResponseResult(IEnumerable<T1> items, T2 summary, Pager pager, int totalCount) : this(items, summary, pager.PageIndex, pager.PageSize, totalCount) { }

    /// <summary>
    /// 成功响应，携带分页数据和汇总信息
    /// </summary>
    /// <param name="items">当前页的数据列表</param>
    /// <param name="summary">汇总数据（如合计值、统计信息等）</param>
    /// <param name="pageIndex">当前页码（从1开始）</param>
    /// <param name="pageSize">每页记录数</param>
    /// <param name="totalCount">总记录数</param>
    public static SummaryPagerResponseResult<T1, T2> Success(IEnumerable<T1> items, T2 summary, int pageIndex, int pageSize, int totalCount)
    {
        return new SummaryPagerResponseResult<T1, T2>(items, summary, pageIndex, pageSize, totalCount) { Message = "操作成功" };
    }

    /// <summary>
    /// 成功响应，携带分页数据和汇总信息
    /// </summary>
    /// <param name="items">当前页的数据列表</param>
    /// <param name="summary">汇总数据（如合计值、统计信息等）</param>
    /// <param name="pager">分页请求参数（包含页码和每页大小）</param>
    /// <param name="totalCount">总记录数</param>
    public static SummaryPagerResponseResult<T1, T2> Success(IEnumerable<T1> items, T2 summary, Pager pager, int totalCount)
    {
        return new SummaryPagerResponseResult<T1, T2>(items, summary, pager, totalCount) { Message = "操作成功" };
    }
    /// <summary>
    /// 错误响应，返回当前泛型类型的实例。
    /// 使用 <see langword="new"/> 隐藏基类方法以返回具体子类型，调用者应始终使用具体类型调用。
    /// 错误码必须为非0值（0 表示成功，与错误语义矛盾）
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">code 为 0 时抛出</exception>
    public static new SummaryPagerResponseResult<T1, T2> Error(int code, string message)
    {
        if (code == 0) throw new ArgumentOutOfRangeException(nameof(code), "错误码不能为 0（0 表示成功）");
        return new SummaryPagerResponseResult<T1, T2> { Code = code, Message = message };
    }
}