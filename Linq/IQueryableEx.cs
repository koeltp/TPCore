
using System.Linq.Expressions;

using Taipi.Core.RQRS;

namespace Taipi.Core.Linq;

public static class IQueryableEx
{
    public static IQueryable<T> Page<T>(this IQueryable<T> source, int pageIndex, int pageSize)
    {
        pageIndex = pageIndex < 0 ? 1 : pageIndex;
        pageSize = pageSize < 0 ? 10 : pageSize;
        return source.Skip((pageIndex - 1) * pageSize).Take(pageSize);
    }

    public static IQueryable<T> Page<T>(this IQueryable<T> source, Pager pager)
    {
        return source.Skip((pager.PageIndex-1) * pager.PageSize).Take(pager.PageSize);
    }

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
}
