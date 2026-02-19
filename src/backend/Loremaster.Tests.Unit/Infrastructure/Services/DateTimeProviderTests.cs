using Loremaster.Infrastructure.Services;
using Loremaster.Application.Common.Interfaces;

namespace Loremaster.Tests.Unit.Infrastructure.Services;

/// <summary>
/// Unit tests for DateTimeProvider.
/// Tests UTC time provision.
/// </summary>
public class DateTimeProviderTests
{
    [Fact]
    public void UtcNow_ShouldReturnCurrentUtcTime()
    {
        // Arrange
        var provider = new DateTimeProvider();
        var before = DateTime.UtcNow;

        // Act
        var result = provider.UtcNow;
        var after = DateTime.UtcNow;

        // Assert
        result.Should().BeOnOrAfter(before);
        result.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void UtcNow_ShouldReturnUtcKind()
    {
        // Arrange
        var provider = new DateTimeProvider();

        // Act
        var result = provider.UtcNow;

        // Assert
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void UtcNow_ShouldReturnConsistentType()
    {
        // Arrange
        var provider = new DateTimeProvider();

        // Act
        var result = provider.UtcNow;

        // Assert
        Assert.IsType<DateTime>(result);
    }

    [Fact]
    public void DateTimeProvider_ShouldImplementIDateTimeProvider()
    {
        // Arrange & Act
        var provider = new DateTimeProvider();

        // Assert
        provider.Should().BeAssignableTo<IDateTimeProvider>();
    }
}
