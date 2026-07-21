using FluentAssertions;

using Netptune.Core.Requests;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Requests;

public class PageRequestTests
{
    [Fact]
    public void GetPagination_ShouldReturnDefaults_WhenValuesAreMissing()
    {
        var request = new PageRequest();

        var pagination = request.GetPagination();

        pagination.Page.Should().Be(PaginationDefaults.DefaultPage);
        pagination.PageSize.Should().Be(PaginationDefaults.DefaultPageSize);
        pagination.Skip.Should().Be(0);
    }

    [Fact]
    public void GetPagination_ShouldClampPageAndPageSize()
    {
        var request = new PageRequest
        {
            Page = 0,
            PageSize = PaginationDefaults.MaxAdminPageSize + 1,
        };

        var pagination = request.GetPagination(PaginationDefaults.MaxAdminPageSize);

        pagination.Page.Should().Be(1);
        pagination.PageSize.Should().Be(PaginationDefaults.MaxAdminPageSize);
    }

    [Fact]
    public void Skip_ShouldNotOverflow()
    {
        var request = new PageRequest
        {
            Page = int.MaxValue,
            PageSize = PaginationDefaults.MaxPageSize,
        };

        var pagination = request.GetPagination();

        pagination.Skip.Should().Be(int.MaxValue);
    }
}
