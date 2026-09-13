namespace Peoplise.SharedKernel.Pagination;

/// <summary>
/// A page request for a query endpoint: which page, and how large. Clamps to sane
/// bounds in the constructor so a handler never has to defend against a zero or
/// negative page size from the caller.
/// </summary>
public sealed class PagedRequest
{
    public const int MaxPageSize = 100;

    public int Page { get; }
    public int PageSize { get; }

    public PagedRequest(int page = 1, int pageSize = 20)
    {
        Page = page < 1 ? 1 : page;
        PageSize = pageSize switch
        {
            < 1 => 20,
            > MaxPageSize => MaxPageSize,
            _ => pageSize,
        };
    }

    public int Skip => (Page - 1) * PageSize;
}
