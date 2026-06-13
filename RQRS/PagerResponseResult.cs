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
    /// 通过分页参数构造响应结果，Code 默认为 0（成功）
    /// </summary>
    public PagerResponseResult(IEnumerable<T> items, int pageIndex, int pageSize, int totalCount)
    {
        Data = new PagerResponse<T>
        {
            Items = items,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// 通过 Pager 对象构造响应结果，Code 默认为 0（成功）
    /// </summary>
    public PagerResponseResult(IEnumerable<T> items, Pager pager, int totalCount) : this(items, pager.PageIndex, pager.PageSize, totalCount) { }

    /// <summary>
    /// 成功响应，携带分页数据
    /// </summary>
    public static PagerResponseResult<T> Success(IEnumerable<T> items, int pageIndex, int pageSize, int totalCount)
    {
        return new PagerResponseResult<T>(items, pageIndex, pageSize, totalCount);
    }

    /// <summary>
    /// 成功响应，通过 Pager 对象携带分页数据
    /// </summary>
    public static PagerResponseResult<T> Success(IEnumerable<T> items, Pager pager, int totalCount)
    {
        return new PagerResponseResult<T>(items, pager, totalCount);
    }

    /// <summary>
    /// 错误响应，业务错误码和消息由调用者指定
    /// </summary>
    public static new PagerResponseResult<T> Error(int code, string message)
    {
        return new PagerResponseResult<T> { Code = code, Message = message };
    }
}
