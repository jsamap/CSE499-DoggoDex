namespace DoggoDex.Shared.Common;

/// <summary>Standard envelope for paged API responses.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>1-based page number that was served.</summary>
    public int Page { get; set; }

    /// <summary>Effective page size after clamping.</summary>
    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}
