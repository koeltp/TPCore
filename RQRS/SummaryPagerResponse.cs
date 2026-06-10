namespace Taipi.Core.RQRS;

/// <summary>
/// 带汇总数据的分页响应类，在标准分页响应基础上附加汇总信息
/// </summary>
/// <typeparam name="T1">分页列表项的数据类型</typeparam>
/// <typeparam name="T2">汇总数据的类型</typeparam>
public class SummaryPagerResponse<T1, T2> : PagerResponse<T1>
{
    /// <summary>汇总数据（如合计值、统计信息等）</summary>
    public T2? Summary { get; set; }
}