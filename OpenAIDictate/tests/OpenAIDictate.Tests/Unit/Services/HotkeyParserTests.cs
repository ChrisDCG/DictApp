using FluentAssertions;
using OpenAIDictate.Services;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Unit tests for HotkeyParser
/// </summary>
public class HotkeyParserTests
{
    [Theory]
    [InlineData("F5")]
    [InlineData("F1")]
    [InlineData("F12")]
    public void Parse_SimpleFunctionKey_ShouldReturnCorrectModifiersAndKey(string gesture)
    {
        // Act
        var (modifiers, virtualKey) = HotkeyParser.Parse(gesture);

        // Assert
        modifiers.Should().Be(0u); // No modifiers
        virtualKey.Should().BeGreaterThan(0u);
    }

    [Theory]
    [InlineData("Ctrl+F5", HotkeyParser.KeyModifier.Control)]
    [InlineData("Alt+F5", HotkeyParser.KeyModifier.Alt)]
    [InlineData("Shift+F5", HotkeyParser.KeyModifier.Shift)]
    [InlineData("Win+F5", HotkeyParser.KeyModifier.Win)]
    public void Parse_SingleModifier_ShouldReturnCorrectModifier(string gesture, HotkeyParser.KeyModifier expectedModifier)
    {
        // Act
        var (modifiers, virtualKey) = HotkeyParser.Parse(gesture);

        // Assert
        modifiers.Should().Be((uint)expectedModifier);
        virtualKey.Should().Be(0x74u); // F5
    }

    [Theory]
    [InlineData("Ctrl+Shift+F5")]
    [InlineData("Alt+Ctrl+F5")]
    [InlineData("Ctrl+Alt+Shift+F10")]
    public void Parse_MultipleModifiers_ShouldReturnCombinedModifiers(string gesture)
    {
        // Act
        var (modifiers, virtualKey) = HotkeyParser.Parse(gesture);

        // Assert
        modifiers.Should().BeGreaterThan(0u);
        virtualKey.Should().BeGreaterThan(0u);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Parse_EmptyGesture_ShouldThrowArgumentException(string? gesture)
    {
        // Act & Assert
        var act = () => HotkeyParser.Parse(gesture!);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("InvalidKey")]
    [InlineData("F99")]
    [InlineData("Ctrl+Invalid")]
    public void Parse_InvalidKey_ShouldThrowArgumentException(string gesture)
    {
        // Act & Assert
        var act = () => HotkeyParser.Parse(gesture);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("F5+F6")] // Multiple keys
    [InlineData("Ctrl+Shift")] // No key
    public void Parse_InvalidFormat_ShouldThrowArgumentException(string gesture)
    {
        // Act & Assert
        var act = () => HotkeyParser.Parse(gesture);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Format_ValidModifiersAndKey_ShouldReturnCorrectGesture()
    {
        // Arrange
        uint modifiers = (uint)(HotkeyParser.KeyModifier.Control | HotkeyParser.KeyModifier.Shift);
        uint virtualKey = 0x74; // F5

        // Act
        var result = HotkeyParser.Format(modifiers, virtualKey);

        // Assert
        result.Should().Be("Ctrl+Shift+F5");
    }

    [Fact]
    public void IsValid_ValidGesture_ShouldReturnTrue()
    {
        // Act
        var result = HotkeyParser.IsValid("Ctrl+F5");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_InvalidGesture_ShouldReturnFalse()
    {
        // Act
        var result = HotkeyParser.IsValid("InvalidKey");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetSuggestions_ShouldReturnNonEmptyList()
    {
        // Act
        var suggestions = HotkeyParser.GetSuggestions();

        // Assert
        suggestions.Should().NotBeEmpty();
        suggestions.Should().Contain("F5");
    }
}

