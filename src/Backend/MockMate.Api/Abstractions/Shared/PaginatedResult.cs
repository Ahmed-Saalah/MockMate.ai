namespace MockMate.Api.Abstractions.Shared;

public class PaginatedResult<TEntity>
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public long Count { get; }
    public IEnumerable<TEntity> Data { get; }

    public PaginatedResult(int pageIndex, int pageSize, long count, IEnumerable<TEntity> data)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        Count = count;
        Data = data;
    }
}
