namespace Taipi.Core.RQRS;

/// <summary>
/// 分页响应数据类，包含当前页数据列表及分页元信息
/// </summary>
/// <typeparam name="T">列表项的数据类型</typeparam>
public class PagerResponse<T>
{
    private int _totalCount;
    private int _pageSize = 10;
    private int _pageIndex = 1;

    /// <summary>当前页的数据列表</summary>
    public IEnumerable<T> Items { get; init; } = [];

    /// <summary>
    /// 总记录数，负值自动修正为0
    /// </summary>
    public int TotalCount
    {
        get => _totalCount;
        set => _totalCount = Math.Max(0, value);
    }

    /// <summary>
    /// 每页记录数，小于等于0时回退为默认值10
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 0 ? value : 10;
    }

    /// <summary>
    /// 当前页码（从1开始），小于1时自动修正为1
    /// </summary>
    public int PageIndex
    {
        get => _pageIndex;
        set => _pageIndex = value < 1 ? 1 : value;
    }

    /// <summary>
    /// 总页数，根据 TotalCount 和 PageSize 自动计算
    /// </summary>
    public int PageCount => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// 是否有上一页（当前页码 &gt; 1）
    /// </summary>
    public bool HasPreviousPage => PageIndex > 1;

    /// <summary>
    /// 是否有下一页（当前页码 &lt; 总页数）
    /// </summary>
    public bool HasNextPage => PageIndex < PageCount;
}