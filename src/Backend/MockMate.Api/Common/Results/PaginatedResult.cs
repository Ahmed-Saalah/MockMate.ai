namespace MockMate.Api.Common.Results;

public sealed record PaginatedResult<T>(
    IEnumerable<T> Data,
    int TotalCount,
    int PageIndex,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => PageIndex < TotalPages;
    public bool HasPreviousPage => PageIndex > 1;
}
