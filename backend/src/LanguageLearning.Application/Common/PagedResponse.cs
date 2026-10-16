
namespace LanguageLearning.Application.Common;

public class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalItems { get; }
    public int TotalPages { get; }
    public bool HasNextPage { get; }
    public bool HasPreviousPage { get; }

    public PagedResponse(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalItems)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page));

        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize));

        if (totalItems < 0)
            throw new ArgumentOutOfRangeException(nameof(totalItems));

        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalItems = totalItems;

        TotalPages = (int)Math.Ceiling(
            (double)totalItems / pageSize);

        HasNextPage = page < TotalPages;
        HasPreviousPage = page > 1 && TotalPages > 0;
    }
}
