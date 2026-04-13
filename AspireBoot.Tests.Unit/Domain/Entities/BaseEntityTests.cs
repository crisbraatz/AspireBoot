using AspireBoot.Domain.Entities;
using FluentAssertions;

namespace AspireBoot.Tests.Unit.Domain.Entities;

public class BaseEntityTests
{
    private const string CreateProperty = "CreateProperty";
    private const string CreateRequestedBy = "CreateRequestedBy";
    private const string UpdateRequestedBy = "UpdateRequestedBy";
    private readonly DateTime _dateTime = DateTime.UtcNow;
    private readonly EntityForTests _entity = new(CreateProperty, CreateRequestedBy);

    [Fact]
    public void ShouldCreateAnEntity()
    {
        _entity.Property.Should().Be(CreateProperty);
        _entity.Id.Should().NotBeEmpty();
        _entity.CreatedAt.Should().BeCloseTo(_dateTime, TimeSpan.FromSeconds(1));
        _entity.CreatedBy.Should().Be(CreateRequestedBy);
        _entity.UpdatedAt.Should().BeCloseTo(_dateTime, TimeSpan.FromSeconds(1));
        _entity.UpdatedBy.Should().Be(CreateRequestedBy);
        _entity.Active.Should().BeTrue();
    }

    [Fact]
    public void ShouldDeactivateAnEntity()
    {
        _entity.Deactivate(UpdateRequestedBy);

        _entity.Property.Should().Be(CreateProperty);
        _entity.Id.Should().NotBeEmpty();
        _entity.CreatedAt.Should().BeCloseTo(_dateTime, TimeSpan.FromSeconds(1));
        _entity.CreatedBy.Should().Be(CreateRequestedBy);
        _entity.UpdatedAt.Should().BeAfter(_entity.CreatedAt);
        _entity.UpdatedBy.Should().Be(UpdateRequestedBy);
        _entity.Active.Should().BeFalse();
    }

    [Fact]
    public void ShouldReactivateAnEntity()
    {
        _entity.Deactivate(UpdateRequestedBy);

        _entity.Reactivate(UpdateRequestedBy);

        _entity.Property.Should().Be(CreateProperty);
        _entity.Id.Should().NotBeEmpty();
        _entity.CreatedAt.Should().BeCloseTo(_dateTime, TimeSpan.FromSeconds(1));
        _entity.CreatedBy.Should().Be(CreateRequestedBy);
        _entity.UpdatedAt.Should().BeAfter(_entity.CreatedAt);
        _entity.UpdatedBy.Should().Be(UpdateRequestedBy);
        _entity.Active.Should().BeTrue();
    }

    [Fact]
    public void ShouldUpdateAnEntity()
    {
        const string updateProperty = "UpdateProperty";

        _entity.UpdateProperty(updateProperty, UpdateRequestedBy);

        _entity.Property.Should().Be(updateProperty);
        _entity.Id.Should().NotBeEmpty();
        _entity.CreatedAt.Should().BeCloseTo(_dateTime, TimeSpan.FromSeconds(1));
        _entity.CreatedBy.Should().Be(CreateRequestedBy);
        _entity.UpdatedAt.Should().BeAfter(_entity.CreatedAt);
        _entity.UpdatedBy.Should().Be(UpdateRequestedBy);
        _entity.Active.Should().BeTrue();
    }

    private sealed class EntityForTests(string property, string requestedBy) : BaseEntity(requestedBy)
    {
        public string Property { get; private set; } = property;

        public void Deactivate(string requestedBy) => SetActiveAs(false, requestedBy);

        public void Reactivate(string requestedBy) => SetActiveAs(true, requestedBy);

        public void UpdateProperty(string property, string requestedBy)
        {
            SetUpdate(requestedBy);
            Property = property;
        }
    }
}
