using AspireBoot.Core.Identity.Auth.SendEmailConfirmation;
using AspireBoot.Infrastructure.Integrations.Rabbit;

namespace AspireBoot.Tests.Unit.Infrastructure.Integrations.Rabbit;

public class RabbitEventMessageTypeNamingTests
{
    [Fact]
    public void ShouldReturnTrueWhenUsingTheDeclaredEventTypeInsteadOfTheClrTypeName()
    {
        bool named = new RabbitEventMessageTypeNaming().TryDetermineName(typeof(SendEmailConfirmationEvent), out string messageTypeName);

        Assert.True(named);
        Assert.Equal("identity.auth.send-email-confirmation", messageTypeName);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(RabbitEventMessageTypeNaming))]
    public void ShouldReturnFalseAndDeclineTypesWithoutAnEventTypeAttribute(Type type)
    {
        bool named = new RabbitEventMessageTypeNaming().TryDetermineName(type, out string messageTypeName);

        Assert.False(named);
        Assert.Empty(messageTypeName);
    }
}
