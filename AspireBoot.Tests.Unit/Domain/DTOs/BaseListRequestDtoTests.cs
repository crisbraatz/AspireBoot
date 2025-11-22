using AspireBoot.Domain.DTOs;
using AspireBoot.Domain.Entities;
using FluentAssertions;

namespace AspireBoot.Tests.Unit.Domain.DTOs;

public class BaseListRequestDtoTests
{
    private DtoForTests? _request;

    [Theory]
    [InlineData(1, 1, 1, 1)]
    [InlineData(2, 2, 2, 2)]
    [InlineData(0, 0, 1, 10)]
    [InlineData(-1, -1, 1, 10)]
    [InlineData(10, 100, 10, 100)]
    [InlineData(10, 101, 10, 100)]
    public void ShouldCreateDtoWithCustomValues(int sentPage, int sentSize, int expectedPage, int expectedSize)
    {
        const string requestedBy = "RequestedBy";
        Guid id = Guid.CreateVersion7();
        DateTime createdAt = DateTime.UtcNow;
        const string createdBy = "CreatedBy";
        DateTime updatedAt = DateTime.UtcNow;
        const string updatedBy = "UpdatedBy";
        const bool active = true;

        _request = new DtoForTests(requestedBy)
        {
            Filters = [x => x.Active],
            OrderBy = new Dictionary<string, bool> { { "id", true } },
            CurrentPage = sentPage,
            Size = sentSize,
            Id = id,
            CreatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy,
            Active = active
        };

        _request.Filters.Should().HaveCount(1);
        _request.OrderBy.Should().HaveCount(1);
        _request.RequestedBy.Should().Be(requestedBy);
        _request.CurrentPage.Should().Be(expectedPage);
        _request.Size.Should().Be(expectedSize);
        _request.Id.Should().Be(id);
        _request.CreatedAt.Should().Be(createdAt);
        _request.CreatedBy.Should().Be(createdBy);
        _request.UpdatedAt.Should().Be(updatedAt);
        _request.UpdatedBy.Should().Be(updatedBy);
        _request.Active.Should().Be(active);
    }

    [Fact]
    public void ShouldCreateDtoWithDefaultValues()
    {
        _request = new DtoForTests();

        _request.Filters.Should().BeEmpty();
        _request.OrderBy.Should().BeEmpty();
        _request.RequestedBy.Should().BeNull();
        _request.CurrentPage.Should().Be(1);
        _request.Size.Should().Be(10);
        _request.Id.Should().BeNull();
        _request.CreatedAt.Should().BeNull();
        _request.CreatedBy.Should().BeNull();
        _request.UpdatedAt.Should().BeNull();
        _request.UpdatedBy.Should().BeNull();
        _request.Active.Should().BeNull();
    }

    private sealed class DtoForTests(string? requestedBy = null)
        : BaseListRequestDto<EntityForTests>(requestedBy);

#pragma warning disable CA1812
    private sealed class EntityForTests(string requestedBy) : BaseEntity(requestedBy);
#pragma warning restore CA1812
}
