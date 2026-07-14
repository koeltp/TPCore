namespace Taipi.Core.RQRS;

/// <summary>
/// 带搜索条件的分页请求类，在基础分页参数上附加搜索条件。
/// Condition 使用 required 修饰符，编译期强制要求赋值
/// </summary>
/// <typeparam name="T">搜索条件的类型</typeparam>
public class SearchPager<T> : Pager
{
    /// <summary>
    /// 搜索/筛选条件，必须在构造时赋值（required 修饰符确保编译期检查）
    /// </summary>
    public required T Condition { get; set; }
}
