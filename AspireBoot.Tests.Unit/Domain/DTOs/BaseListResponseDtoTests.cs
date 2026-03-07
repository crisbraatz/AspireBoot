using AspireBoot.Domain.DTOs;
using FizzWare.NBuilder;
using FluentAssertions;

namespace AspireBoot.Tests.Unit.Domain.DTOs;

public class BaseListResponseDtoTests
{
    private ListDtoForTests? _response;

    [Theory]
    [InlineData(10, 1, 10, 10, 1)]
    [InlineData(10, 2, 10, 20, 2)]
    [InlineData(10, 3, 10, 31, 4)]
    public void ShouldCreateDtoWithManyData(
        int listSize, int currentPage, int size, int totalItems, int expectedTotalPages)
    {
        _response = new ListDtoForTests(
            Builder<DtoForTests>.CreateListOfSize(listSize).Build().ToList(), currentPage, size, totalItems);

        _response.Data.Should().HaveCount(listSize);
        _response.CurrentPage.Should().Be(currentPage);
        _response.TotalPages.Should().Be(expectedTotalPages);
        _response.TotalItems.Should().Be(totalItems);
    }

    [Fact]
    public void ShouldCreateDtoWithSingleData()
    {
        _response = new ListDtoForTests(new DtoForTests());

        _response.Data.Should().HaveCount(1);
        _response.CurrentPage.Should().Be(1);
        _response.TotalPages.Should().Be(1);
        _response.TotalItems.Should().Be(1);
    }

    [Fact]
    public void ShouldCreateEmptyDto()
    {
        _response = new ListDtoForTests();

        _response.Data.Should().BeEmpty();
        _response.CurrentPage.Should().Be(1);
        _response.TotalPages.Should().Be(1);
        _response.TotalItems.Should().Be(0);
    }

    private sealed class ListDtoForTests : BaseListResponseDto<DtoForTests>
    {
        public ListDtoForTests(IEnumerable<DtoForTests> data, int currentPage, int size, int totalItems)
            : base(data, currentPage, size, totalItems)
        {
        }

        public ListDtoForTests(DtoForTests data) : base(data)
        {
        }

        public ListDtoForTests()
        {
        }
    }

#pragma warning disable S2094
    private sealed class DtoForTests;
#pragma warning restore S2094
}
