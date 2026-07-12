using Taipi.Core.Exceptions;

namespace Taipi.Core.RQRS;

/// <summary>
/// 分页请求参数基类，封装页码、每页大小及排序条件
/// </summary>
public class Pager
{
    private int pageIndex;
    private int pageSize;

    /// <summary>
    /// 当前页码（从1开始），小于1时自动修正为1
    /// </summary>
    public int PageIndex
    {
        get => pageIndex;
        set => pageIndex = value < 1 ? 1 : value;
    }

    /// <summary>
    /// 每页记录数，小于等于0时自动修正为10
    /// </summary>
    public int PageSize
    {
        get { return pageSize; }
        set => pageSize = value <= 0 ? 10 : value;
    }

    /// <summary>排序条件列表，可为空表示不排序</summary>
    public List<OrderByRQ>? OrderBy { get; set; }
}

/// <summary>
/// 排序条件请求类，描述单个字段的排序规则
/// </summary>
public class OrderByRQ
{
    private string _field = string.Empty;

    /// <summary>
    /// 排序字段名，仅允许字母、数字、下划线、点号及方括号，
    /// 赋值时严格校验以防止 SQL 注入
    /// </summary>
    /// <exception cref="ValidationException">字段名包含非法字符时抛出</exception>
    public string Field
    {
        get => _field;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _field = string.Empty;
                return;
            }
            // 白名单校验：仅允许合法的数据库标识符字符，拦截 SQL 注入
            if (!IsValidFieldName(value))
                throw new ValidationException(AppCodes.InvalidSortField, $"非法排序字段名: {value}");
            _field = value.Trim();
        }
    }

    /// <summary>排序方式：0 表示升序（ASC），1 表示降序（DESC）。默认值为 0。</summary>
    public int Type { get; set; }

    /// <summary>
    /// 校验字段名是否仅包含合法字符（字母、数字、下划线、点号、方括号）
    /// </summary>
    private static bool IsValidFieldName(string field)
    {
        // 允许: 字母(含中文)、数字、_ . []，拒绝空格、分号、引号、注释符号等一切 SQL 注入字符
        return field.Trim().All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '[' || c == ']');
    }
}

/// <summary>
/// 分页扩展工具类，提供将 OrderByRQ 列表转换为 SQL ORDER BY 子句的方法
/// </summary>
public static class PagerEx
{
    /// <summary>
    /// 将排序条件列表转换为 SQL ORDER BY 子句字符串
    /// </summary>
    /// <param name="items">排序条件列表</param>
    /// <returns>SQL ORDER BY 子句，无排序条件时返回空字符串</returns>
    public static string ToSql(this List<OrderByRQ> items)
    {
        if (items == null || items.Count == 0)
            return string.Empty;

        // Field 已在 setter 中完成白名单校验，此处无需重复校验
        var orderByStr = string.Empty;
        items.ForEach(item =>
        {
            if (string.IsNullOrEmpty(item.Field)) return;
            orderByStr += $"{item.Field} {(item.Type == 0 ? "ASC" : "DESC")},";
        });

        return orderByStr.TrimEnd(',');
    }
}
