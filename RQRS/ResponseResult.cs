namespace Taipi.Core.RQRS;

public class ResponseResult<T> : StatusResponseResult
{
    public T? Data { get; set; }

    public ResponseResult() { }
    public ResponseResult(T data) { Data = data; }

    public static new ResponseResult<T> Success()
    {
        return new ResponseResult<T> { Code = 200, Message = "操作成功" };
    }

    public static new ResponseResult<T> Success(string message)
    {
        return new ResponseResult<T> { Code = 200, Message = message };
    }

    public static new ResponseResult<T> Error(int code, string message)
    {
        return new ResponseResult<T> { Code = code, Message = message };
    }

    public static new ResponseResult<T> BadRequest(string message = "请求参数错误")
    {
        return Error(400, message);
    }

    public static new ResponseResult<T> Unauthorized(string message = "未授权")
    {
        return Error(401, message);
    }

    public static new ResponseResult<T> Forbidden(string message = "禁止访问")
    {
        return Error(403, message);
    }

    public static new ResponseResult<T> NotFound(string message = "资源未找到")
    {
        return Error(404, message);
    }

    public static new ResponseResult<T> InternalError(string message = "服务器内部错误")
    {
        return Error(500, message);
    }
}