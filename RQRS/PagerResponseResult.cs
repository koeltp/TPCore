namespace Taipi.Core.RQRS;

/// <summary>
/// 分页响应结果类，在状态响应基础上携带分页数据
/// </summary>
/// <typeparam name="T">分页列表项的数据类型</typeparam>
public class PagerResponseResult<T> : StatusResponseResult
{
    /// <summary>分页响应数据（包含列表项、分页信息等）</summary>
    public PagerResponse<T>? Data { get; set; }

    /// <summary>无参构造函数，用于反序列化或后续手动赋值场景</summary>
    public PagerResponseResult() { }

    /// <summary>
    /// 通过分页参数构造响应结果，Code 默认为 0（成功），Message 默认为"操作成功"
    /// </summary>
    public PagerResponseResult(IEnumerable<T> items, int pageIndex, int pageSize, int totalCount)
    {
        Message = "操作成功";
        Data = new PagerResponse<T>
        {
            Items = items,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// 通过 Pager 对象构造响应结果，Code 默认为 0（成功），Message 默认为"操作成功"
    /// </summary>
    public PagerResponseResult(IEnumerable<T> items, Pager pager, int totalCount) : this(items, pager.PageIndex, pager.PageSize, totalCount) { }

    /// <summary>
    /// 成功响应，携带分页数据
    /// </summary>
    public static PagerResponseResult<T> Success(IEnumerable<T> items, int pageIndex, int pageSize, int totalCount)
    {
        return new PagerResponseResult<T>(items, pageIndex, pageSize, totalCount) { Message = "操作成功" };
    }

    /// <summary>
    /// 成功响应，通过 Pager 对象携带分页数据
    /// </summary>
    public static PagerResponseResult<T> Success(IEnumerable<T> items, Pager pager, int totalCount)
    {
        return new PagerResponseResult<T>(items, pager, totalCount) { Message = "操作成功" };
    }

    /// <summary>
    /// 错误响应，返回当前泛型类型的实例。
    /// 使用 <see langword="new"/> 隐藏基类方法以返回具体子类型，调用者应始终使用具体类型调用。
    /// 错误码必须为非0值（0 表示成功，与错误语义矛盾）
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">code 为 0 时抛出</exception>
    public static new PagerResponseResult<T> Error(int code, string message)
    {
        if (code == 0) throw new ArgumentOutOfRangeException(nameof(code), "错误码不能为 0（0 表示成功）");
        return new PagerResponseResult<T> { Code = code, Message = message };
    }
}
