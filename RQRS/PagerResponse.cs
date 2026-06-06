namespace Taipi.Core.RQRS;

public class PagerResponse<T>
{
    private int _totalCount;
    private int _pageSize = 10;
    private int _pageIndex = 1;

    public IEnumerable<T> Items { get; set; } = Array.Empty<T>();

    public int TotalCount
    {
        get => _totalCount;
        set => _totalCount = Math.Max(0, value);
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 0 ? value : 10;
    }

    public int PageIndex
    {
        get => _pageIndex;
        set => _pageIndex = value < 1 ? 1 : value;
    }

    public int PageCount => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}