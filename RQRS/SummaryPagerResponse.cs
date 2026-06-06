namespace Taipi.Core.RQRS;

public class SummaryPagerResponse<T1, T2> : PagerResponse<T1>
{
    public T2? Summary { get; set; }
}