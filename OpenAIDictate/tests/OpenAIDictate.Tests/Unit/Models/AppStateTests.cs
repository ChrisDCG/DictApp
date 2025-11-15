using FluentAssertions;
using OpenAIDictate.Models;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Models;

/// <summary>
/// Unit tests for AppState enum
/// </summary>
public class AppStateTests
{
    [Fact]
    public void AppState_Values_ShouldBeDefined()
    {
        // Assert
        Enum.GetValues<AppState>().Should().Contain(AppState.Idle);
        Enum.GetValues<AppState>().Should().Contain(AppState.Recording);
        Enum.GetValues<AppState>().Should().Contain(AppState.Transcribing);
    }

    [Fact]
    public void AppState_DefaultValue_ShouldBeIdle()
    {
        // Act
        var state = default(AppState);

        // Assert
        state.Should().Be(AppState.Idle);
    }
}

