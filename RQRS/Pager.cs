namespace Taipi.Core.RQRS;

public class Pager
{
    private int pageIndex;
    private int pageSize;

    public int PageIndex
    {
        get => pageIndex;
        set => pageIndex = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get { return pageSize; }
        set => pageSize = value <= 0 ? 10 : value;
    }

    public List<OrderByRQ>? OrderBy { get; set; }
}


public class OrderByRQ
{
    /// <summary>
    /// 字段名称，必须是数据库表中的有效字段名，且不能包含空格或特殊字符
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// 0 表示升序（ASC），1 表示降序（DESC）。默认值为 0。
    /// </summary>
    public int Type { get; set; }
}

/// <summary>
/// 分页扩展类，提供将 OrderByRQ 列表转换为 SQL ORDER BY 子句的方法
/// </summary>
public static class PagerEx
{
    public static string ToSql(this List<OrderByRQ> items)
    {
        if (items == null || !items.Any())
        {
            return string.Empty;
        }

        var orderByStr = string.Empty;
        items.ForEach(item =>
        {
            if (item.Field.Trim().Any(char.IsWhiteSpace))
            {
                throw new InvalidOperationException($"Invalid field name: {item.Field}");
            }

            orderByStr += $"{item.Field.Trim()} {(item.Type == 0 ? "ASC" : "DESC")},";
        });

        return orderByStr.TrimEnd(',');
    }
}
