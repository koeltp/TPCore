using System.Linq.Expressions;

using Taipi.Core.RQRS;

namespace Taipi.Core.Linq;

/// <summary>
/// IQueryable 扩展方法
/// </summary>
public static class IQueryableEx
{
    /// <summary>
    /// 基于页码和页大小进行分页
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="source">数据源</param>
    /// <param name="pageIndex">页码（从 1 开始，小于 1 自动修正为 1）</param>
    /// <param name="pageSize">页大小（小于等于 0 自动修正为 10）</param>
    /// <returns>分页后的数据</returns>
    public static IQueryable<T> Page<T>(this IQueryable<T> source, int pageIndex, int pageSize)
    {
        pageIndex = pageIndex < 1 ? 1 : pageIndex;
        pageSize = pageSize <= 0 ? 10 : pageSize;
        return source.Skip((pageIndex - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// 基于 Pager 对象进行分页
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="source">数据源</param>
    /// <param name="pager">分页参数</param>
    /// <returns>分页后的数据</returns>
    public static IQueryable<T> Page<T>(this IQueryable<T> source, Pager pager)
    {
        return source.Skip((pager.PageIndex - 1) * pager.PageSize).Take(pager.PageSize);
    }

    /// <summary>
    /// 满足条件时执行 Where 过滤，否则返回原始查询
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="source">数据源</param>
    /// <param name="condition">是否过滤</param>
    /// <param name="predicate">过滤条件</param>
    /// <returns>过滤后的查询</returns>
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition, Expression<Func<T, bool>> predicate)
    {
        return condition ? source.Where(predicate) : source;
    }

    /// <summary>
    /// 根据条件排序
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <typeparam name="TKey">排序键类型</typeparam>
    /// <param name="source">数据源</param>
    /// <param name="condition">是否排序</param>
    /// <param name="keySelector">排序键选择器 x => x.Id</param>
    /// <returns>排序后的数据</returns>
    public static IQueryable<T> OrderByIf<T, TKey>(this IQueryable<T> source, bool condition, Expression<Func<T, TKey>> keySelector)
    {
        return condition ? source.OrderBy(keySelector) : source;
    }

    /// <summary>
    /// 根据条件降序排序
    /// </summary>
    public static IQueryable<T> OrderByDescendingIf<T, TKey>(this IQueryable<T> source, bool condition, Expression<Func<T, TKey>> keySelector)
    {
        return condition ? source.OrderByDescending(keySelector) : source;
    }

    /// <summary>
    /// 根据条件追加升序排序（需在 OrderBy 之后使用）
    /// </summary>
    public static IQueryable<T> ThenByIf<T, TKey>(this IQueryable<T> source, bool condition, Expression<Func<T, TKey>> keySelector)
    {
        return condition ? ((IOrderedQueryable<T>)source).ThenBy(keySelector) : source;
    }

    /// <summary>
    /// 根据条件追加降序排序（需在 OrderBy 之后使用）
    /// </summary>
    public static IQueryable<T> ThenByDescendingIf<T, TKey>(this IQueryable<T> source, bool condition, Expression<Func<T, TKey>> keySelector)
    {
        return condition ? ((IOrderedQueryable<T>)source).ThenByDescending(keySelector) : source;
    }
}
