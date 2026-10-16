
using LanguageLearning.Application.Common;
using Xunit;

namespace LanguageLearning.Tests;

public class PaginationTests
{
    [Fact]
    public void PaginationParams_UsesDefaultValues()
    {
        var pagination = new PaginationParams();

        Assert.Equal(1, pagination.Page);
        Assert.Equal(10, pagination.PageSize);
    }

    [Fact]
    public void PaginationParams_LimitsPageSizeTo50()
    {
        var pagination = new PaginationParams
        {
            Page = 0,
            PageSize = 100
        };

        Assert.Equal(1, pagination.Page);
        Assert.Equal(50, pagination.PageSize);
    }

    [Fact]
    public void PagedResponse_CalculatesMetadataCorrectly()
    {
        var items = new List<string> { "Deck A", "Deck B" };

        var response = new PagedResponse<string>(
            items,
            page: 2,
            pageSize: 10,
            totalItems: 25);

        Assert.Equal(2, response.Page);
        Assert.Equal(10, response.PageSize);
        Assert.Equal(25, response.TotalItems);
        Assert.Equal(3, response.TotalPages);
        Assert.True(response.HasNextPage);
        Assert.True(response.HasPreviousPage);
        Assert.Equal(2, response.Items.Count);
    }

    [Fact]
    public void PagedResponse_HandlesEmptyResults()
    {
        var response = new PagedResponse<string>(
            new List<string>(),
            page: 1,
            pageSize: 10,
            totalItems: 0);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalPages);
        Assert.False(response.HasNextPage);
        Assert.False(response.HasPreviousPage);
    }
}
