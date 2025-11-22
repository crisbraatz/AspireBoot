using AspireBoot.Infrastructure.Extensions;
using FizzWare.NBuilder;
using FluentAssertions;

namespace AspireBoot.Tests.Unit.Infrastructure.Extensions;

public class QueryableExtensionTests
{
    private readonly IEnumerable<DtoForTests> _dto = Builder<DtoForTests>
        .CreateListOfSize(10)
        .All()
        .Do(x => x.Property = Guid.NewGuid().ToString())
        .Build()
        .ToList();

    private readonly IEnumerable<int> _intList = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
    private const string PropertyName = "Property";

    [Fact]
    public void ShouldGetAscendingOrderedQueryable()
    {
        IOrderedEnumerable<DtoForTests> expectedList = _dto.OrderBy(x => x.Property);

        IQueryable<DtoForTests> returnedList = _dto.AsQueryable().OrderBy(PropertyName, true);

        returnedList.SequenceEqual(expectedList).Should().BeTrue();
    }

    [Fact]
    public void ShouldGetDescendingOrderedQueryable()
    {
        IOrderedEnumerable<DtoForTests> expectedList = _dto.OrderByDescending(x => x.Property);

        IQueryable<DtoForTests> returnedList = _dto.AsQueryable().OrderBy(PropertyName, false);

        returnedList.SequenceEqual(expectedList).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("InvalidProperty")]
    public void ShouldGetUnorderedQueryableWhenInvalidPropertyProvided(string? property)
    {
        IOrderedEnumerable<DtoForTests> expectedList = _dto.OrderBy(x => x.Property);

        IQueryable<DtoForTests> returnedList = _dto.AsQueryable().OrderBy(property, true);

        returnedList.SequenceEqual(expectedList).Should().BeFalse();
    }

    [Theory]
    [InlineData(1, 5, 5)]
    [InlineData(1, 10, 10)]
    [InlineData(1, 20, 10)]
    [InlineData(2, 5, 5)]
    [InlineData(2, 10, 0)]
    [InlineData(3, 5, 0)]
    public void ShouldGetPaginatedQueryable(int page, int sentSize, int expectedSize)
    {
        IEnumerable<int> list = _intList.AsQueryable().PaginateBy(page, sentSize).ToList();

        list.Should().HaveCount(expectedSize);
    }

#pragma warning disable CA1812
    private sealed class DtoForTests
#pragma warning restore CA1812
    {
        public string? Property { get; set; }
    }
}
