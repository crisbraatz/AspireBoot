using AspireBoot.ApiService.Helpers;
using FluentAssertions;

namespace AspireBoot.Tests.Unit.Api.Helpers;

public class OrderByHelperTests
{
    [Theory, MemberData(nameof(CasesOrderBy))]
    public void ShouldConvertNonEmptyOrderByStringToNonEmptyOrderByDictionary(
        string sentString, IDictionary<string, bool> expectedDictionary)
    {
        IDictionary<string, bool> returnedDictionary = OrderByHelper.ToDictionary<DtoForTests>(sentString);

        returnedDictionary.Should().BeEquivalentTo(expectedDictionary);
    }

    [Fact]
    public void ShouldConvertEmptyOrderByStringToEmptyOrderByDictionary()
    {
        IDictionary<string, bool> dictionary = OrderByHelper.ToDictionary<DtoForTests>(string.Empty);

        dictionary.Should().BeEquivalentTo(new Dictionary<string, bool>());
    }

    [Fact]
    public void ShouldConvertNullOrderByStringToDefaultOrderByDictionary()
    {
        IDictionary<string, bool> dictionary = OrderByHelper.ToDictionary<DtoForTests>(null);

        dictionary.Should().BeEquivalentTo(new Dictionary<string, bool> { { "FIRSTPROPERTY", true } });
    }

    public static IEnumerable<object[]> CasesOrderBy() => new List<object[]>
    {
        new object[] { "FIRSTPROPERTY ASC", new Dictionary<string, bool> { { "FIRSTPROPERTY", true } } },
        new object[] { "secondproperty DESC", new Dictionary<string, bool> { { "SECONDPROPERTY", false } } },
        new object[]
        {
            "FirstProperty ASC;SECONDPROPERTY DESC",
            new Dictionary<string, bool> { { "FIRSTPROPERTY", true }, { "SECONDPROPERTY", false } }
        },
        new object[]
        {
            "SecondProperty DESC;FIRSTPROPERTY ASC",
            new Dictionary<string, bool> { { "SECONDPROPERTY", false }, { "FIRSTPROPERTY", true } }
        }
    };

#pragma warning disable S1144
#pragma warning disable CA1812
    private sealed class DtoForTests
#pragma warning restore CA1812
    {
        public string? FirstProperty { get; init; }
        public string? SecondProperty { get; init; }
    }
#pragma warning restore S1144
}
