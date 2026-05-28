namespace ReportEngine.SharedKernel;

/// <summary>
/// A paged result envelope returned by list queries.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The current page of items.</param>
/// <param name="TotalCount">Total number of matching items across all pages.</param>
/// <param name="Page">Current 1-based page number.</param>
/// <param name="PageSize">Number of items per page.</param>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    /// <summary>Total number of pages.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary><see langword="true"/> when there is a next page.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary><see langword="true"/> when there is a previous page.</summary>
    public bool HasPreviousPage => Page > 1;
}
