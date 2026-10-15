
namespace LanguageLearning.Application.DTOs.Decks;

public class PagedDeckResponse
{
    public List<DeckResponse> Items { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    public bool HasNextPage { get; set; }

    public bool HasPreviousPage { get; set; }
}
