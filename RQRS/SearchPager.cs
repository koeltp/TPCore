namespace Taipi.Core.RQRS;

public class SearchPager<T> : Pager
{
    /// <summary>
    /// 搜索条件
    /// </summary>
    public T Condition { get; set; } = default!;
}
