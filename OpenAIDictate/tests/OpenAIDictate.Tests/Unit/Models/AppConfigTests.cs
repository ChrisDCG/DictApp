using FluentAssertions;
using OpenAIDictate.Models;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Models;

/// <summary>
/// Unit tests for AppConfig model
/// </summary>
public class AppConfigTests
{
    [Fact]
    public void AppConfig_DefaultValues_ShouldBeSet()
    {
        // Act
        var config = new AppConfig();

        // Assert
        config.Model.Should().Be("gpt-4o-transcribe");
        config.HotkeyGesture.Should().Be("F5");
        config.MaxRecordingMinutes.Should().Be(10);
        config.Language.Should().Be("de");
        config.EnablePostProcessing.Should().BeTrue();
        config.EnableVAD.Should().BeTrue();
        config.SilenceThresholdDb.Should().Be(-20.0);
        config.HotkeyVirtualKey.Should().Be(0x74); // F5
    }

    [Fact]
    public void AppConfig_Properties_ShouldBeSettable()
    {
        // Arrange
        var config = new AppConfig
        {
            Model = "gpt-4o-mini-transcribe",
            HotkeyGesture = "Ctrl+F5",
            MaxRecordingMinutes = 15,
            Language = "en",
            EnablePostProcessing = false,
            EnableVAD = false,
            SilenceThresholdDb = -30.0,
            Glossary = "Test glossary"
        };

        // Assert
        config.Model.Should().Be("gpt-4o-mini-transcribe");
        config.HotkeyGesture.Should().Be("Ctrl+F5");
        config.MaxRecordingMinutes.Should().Be(15);
        config.Language.Should().Be("en");
        config.EnablePostProcessing.Should().BeFalse();
        config.EnableVAD.Should().BeFalse();
        config.SilenceThresholdDb.Should().Be(-30.0);
        config.Glossary.Should().Be("Test glossary");
    }
}

