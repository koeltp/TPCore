namespace Taipi.Core.RQRS;

/// <summary>
/// 带搜索条件的分页请求类，在基础分页参数上附加搜索条件
/// </summary>
/// <typeparam name="T">搜索条件的类型</typeparam>
public class SearchPager<T> : Pager
{
    /// <summary>搜索/筛选条件</summary>
    public T Condition { get; set; } = default!;
}
