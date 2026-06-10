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
    /// 通过分页参数构造响应结果，Code 默认为 200
    /// </summary>
    /// <param name="items">当前页的数据列表</param>
    /// <param name="pageIndex">当前页码（从1开始）</param>
    /// <param name="pageSize">每页记录数</param>
    /// <param name="totalCount">总记录数</param>
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
    /// 通过 Pager 对象构造响应结果，Code 默认为 200
    /// </summary>
    /// <param name="items">当前页的数据列表</param>
    /// <param name="pager">分页请求参数（包含页码和每页大小）</param>
    /// <param name="totalCount">总记录数</param>
    public PagerResponseResult(IEnumerable<T> items, Pager pager, int totalCount) : this(items, pager.PageIndex, pager.PageSize, totalCount) { }

    /// <summary>
    /// 成功响应（状态码200），携带分页数据
    /// </summary>
    /// <param name="items">当前页的数据列表</param>
    /// <param name="pageIndex">当前页码（从1开始）</param>
    /// <param name="pageSize">每页记录数</param>
    /// <param name="totalCount">总记录数</param>
    public static PagerResponseResult<T> Success(IEnumerable<T> items, int pageIndex, int pageSize, int totalCount)
    {
        return new PagerResponseResult<T>(items, pageIndex, pageSize, totalCount);
    }

    /// <summary>
    /// 成功响应（状态码200），通过 Pager 对象携带分页数据
    /// </summary>
    /// <param name="items">当前页的数据列表</param>
    /// <param name="pager">分页请求参数</param>
    /// <param name="totalCount">总记录数</param>
    public static PagerResponseResult<T> Success(IEnumerable<T> items, Pager pager, int totalCount)
    {
        return new PagerResponseResult<T>(items, pager, totalCount);
    }

    /// <summary>
    /// 错误响应，状态码与消息均由调用者指定
    /// </summary>
    /// <param name="code">错误状态码</param>
    /// <param name="message">错误描述信息</param>
    public static new PagerResponseResult<T> Error(int code, string message)
    {
        return new PagerResponseResult<T> { Code = code, Message = message };
    }

    /// <summary>请求参数错误（状态码400），默认消息为"请求参数错误"</summary>
    public static new PagerResponseResult<T> BadRequest(string message = "请求参数错误")
    {
        return Error(400, message);
    }

    /// <summary>未授权（状态码401），默认消息为"未授权"</summary>
    public static new PagerResponseResult<T> Unauthorized(string message = "未授权")
    {
        return Error(401, message);
    }

    /// <summary>禁止访问（状态码403），默认消息为"禁止访问"</summary>
    public static new PagerResponseResult<T> Forbidden(string message = "禁止访问")
    {
        return Error(403, message);
    }

    /// <summary>资源未找到（状态码404），默认消息为"资源未找到"</summary>
    public static new PagerResponseResult<T> NotFound(string message = "资源未找到")
    {
        return Error(404, message);
    }

    /// <summary>服务器内部错误（状态码500），默认消息为"服务器内部错误"</summary>
    public static new PagerResponseResult<T> InternalError(string message = "服务器内部错误")
    {
        return Error(500, message);
    }
}