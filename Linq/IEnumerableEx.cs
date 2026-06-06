
using System.Collections.Concurrent;
using Taipi.Core.RQRS;

namespace Taipi.Core.Linq;

public static class IEnumerableEx
{
    /// <summary>
    /// 分页 - 基于页码和页大小进行分页
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    public static IEnumerable<T> Page<T>(this IEnumerable<T> source, int pageIndex, int pageSize)
    {
        pageIndex = pageIndex < 0 ? 1 : pageIndex;
        pageSize = pageSize < 0 ? 10 : pageSize;

        return source.Skip((pageIndex - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// 分页 - 基于Pager对象进行分页
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="pager"></param>
    /// <returns></returns>
    public static IEnumerable<T> Page<T>(this IEnumerable<T> source, Pager pager)
    {
        return source.Skip((pager.PageIndex - 1) * pager.PageSize).Take(pager.PageSize);
    }

    /// <summary>
    ///如果满足条件，则执行Where过滤，否则返回原始集合
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="condition"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> predicate)
    {
        return condition ? source.Where(predicate) : source;
    }

    /// <summary>
    /// 并行执行ForEach操作，适用于需要对集合中的每个元素执行异步操作的场景
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    public static Task ParallelForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> func)
    {
        return Task.WhenAll(source.Select(arg => Task.Run(() => func(arg))));
    }

    public static Task ParallelForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> func, int dop)
    {

        return Task.WhenAll(
            Partitioner.Create(source).GetPartitions(dop)
            .Select(partition => Task.Run(async () =>
            {
                using (partition)
                {
                    while (partition.MoveNext())
                    {
                        await func(partition.Current);
                    }
                }
            })));
    }
}
