namespace Taipi.Core.RQRS;

public class SummaryPagerResponseResult<T1, T2> : StatusResponseResult
{
    public SummaryPagerResponse<T1, T2>? Data { get; set; }

    public SummaryPagerResponseResult() { }

    public SummaryPagerResponseResult(IEnumerable<T1> items, T2 summary, int pageIndex, int pageSize, int totalCount)
    {
        Data = new SummaryPagerResponse<T1, T2>
        {
            Items = items,
            Summary = summary,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public static new SummaryPagerResponseResult<T1, T2> Success()
    {
        return new SummaryPagerResponseResult<T1, T2> { Code = 200, Message = "操作成功" };
    }

    public static new SummaryPagerResponseResult<T1, T2> Success(string message)
    {
        return new SummaryPagerResponseResult<T1, T2> { Code = 200, Message = message };
    }

    public static new SummaryPagerResponseResult<T1, T2> Error(int code, string message)
    {
        return new SummaryPagerResponseResult<T1, T2> { Code = code, Message = message };
    }

    public static new SummaryPagerResponseResult<T1, T2> BadRequest(string message = "请求参数错误")
    {
        return Error(400, message);
    }

    public static new SummaryPagerResponseResult<T1, T2> Unauthorized(string message = "未授权")
    {
        return Error(401, message);
    }

    public static new SummaryPagerResponseResult<T1, T2> Forbidden(string message = "禁止访问")
    {
        return Error(403, message);
    }

    public static new SummaryPagerResponseResult<T1, T2> NotFound(string message = "资源未找到")
    {
        return Error(404, message);
    }

    public static new SummaryPagerResponseResult<T1, T2> InternalError(string message = "服务器内部错误")
    {
        return Error(500, message);
    }
}