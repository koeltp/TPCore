using System.Collections.Concurrent;
using Taipi.Core.RQRS;

namespace Taipi.Core.Linq;

/// <summary>
/// IEnumerable 扩展方法
/// </summary>
public static class IEnumerableEx
{
    /// <summary>
    /// 基于页码和页大小进行分页
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="source">数据源</param>
    /// <param name="pageIndex">页码（从 1 开始，小于 1 自动修正为 1）</param>
    /// <param name="pageSize">页大小（小于等于 0 自动修正为 10）</param>
    /// <returns>分页后的数据</returns>
    public static IEnumerable<T> Page<T>(this IEnumerable<T> source, int pageIndex, int pageSize)
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
    public static IEnumerable<T> Page<T>(this IEnumerable<T> source, Pager pager)
    {
        return source.Skip((pager.PageIndex - 1) * pager.PageSize).Take(pager.PageSize);
    }

    /// <summary>
    /// 满足条件时执行 Where 过滤，否则返回原始集合
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="source">数据源</param>
    /// <param name="condition">是否过滤</param>
    /// <param name="predicate">过滤条件</param>
    /// <returns>过滤后的集合</returns>
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> predicate)
    {
        return condition ? source.Where(predicate) : source;
    }

    /// <summary>
    /// 并行执行 ForEach 操作，适用于需要对集合中的每个元素执行异步操作的场景
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="source">数据源</param>
    /// <param name="func">对每个元素执行的异步操作</param>
    /// <returns>所有任务完成后的 Task</returns>
    public static Task ParallelForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> func)
    {
        return Task.WhenAll(source.Select(arg => Task.Run(() => func(arg))));
    }

    /// <summary>
    /// 限制并发数的并行 ForEach 操作
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="source">数据源</param>
    /// <param name="func">对每个元素执行的异步操作</param>
    /// <param name="maxConcurrency">最大并发数</param>
    /// <returns>所有任务完成后的 Task</returns>
    public static Task ParallelForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> func, int maxConcurrency)
    {
        return Task.WhenAll(
            Partitioner.Create(source).GetPartitions(maxConcurrency)
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
