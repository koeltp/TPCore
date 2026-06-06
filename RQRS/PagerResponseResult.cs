namespace Taipi.Core.RQRS;

public class PagerResponseResult<T> : StatusResponseResult
{
    public PagerResponse<T>? Data { get; set; }

    public PagerResponseResult() { }
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

    public PagerResponseResult(IEnumerable<T> items, Pager pager, int totalCount)
    {
        Data = new PagerResponse<T>
        {
            Items = items,
            PageIndex = pager.PageIndex,
            PageSize = pager.PageSize,
            TotalCount = totalCount
        };
    }

    public static new PagerResponseResult<T> Success()
    {
        return new PagerResponseResult<T> { Code = 200, Message = "操作成功" };
    }

    public static new PagerResponseResult<T> Success(string message)
    {
        return new PagerResponseResult<T> { Code = 200, Message = message };
    }

    public static new PagerResponseResult<T> Error(int code, string message)
    {
        return new PagerResponseResult<T> { Code = code, Message = message };
    }

    public static new PagerResponseResult<T> BadRequest(string message = "请求参数错误")
    {
        return Error(400, message);
    }

    public static new PagerResponseResult<T> Unauthorized(string message = "未授权")
    {
        return Error(401, message);
    }

    public static new PagerResponseResult<T> Forbidden(string message = "禁止访问")
    {
        return Error(403, message);
    }

    public static new PagerResponseResult<T> NotFound(string message = "资源未找到")
    {
        return Error(404, message);
    }

    public static new PagerResponseResult<T> InternalError(string message = "服务器内部错误")
    {
        return Error(500, message);
    }
}