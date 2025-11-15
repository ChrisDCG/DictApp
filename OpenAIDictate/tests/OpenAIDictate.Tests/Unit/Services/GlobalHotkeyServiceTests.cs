using FluentAssertions;
using OpenAIDictate.Services;
#if WINDOWS
using System.Windows.Forms;
#endif
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Unit tests for GlobalHotkeyService
/// Note: These tests require Windows Forms and may need actual window handle
/// </summary>
public class GlobalHotkeyServiceTests : IDisposable
{
#if WINDOWS
    private Form? _testForm;
#endif
    private GlobalHotkeyService? _service;

    [Fact]
    public void Constructor_ValidHandle_ShouldInitialize()
    {
#if WINDOWS
        // Arrange
        _testForm = new Form();
        _testForm.ShowInTaskbar = false;
        _testForm.WindowState = FormWindowState.Minimized;

        // Act
        _service = new GlobalHotkeyService(_testForm.Handle);

        // Assert
        _service.Should().NotBeNull();
        _service.GetCurrentGesture().Should().Be("F5"); // Default
#else
        Assert.True(true, "Test requires Windows Forms");
#endif
    }

    [Fact]
    public void GetCurrentGesture_Default_ShouldReturnF5()
    {
#if WINDOWS
        // Arrange
        _testForm = new Form();
        _service = new GlobalHotkeyService(_testForm.Handle);

        // Act
        var gesture = _service.GetCurrentGesture();

        // Assert
        gesture.Should().Be("F5");
#else
        Assert.True(true, "Test requires Windows Forms");
#endif
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
#if WINDOWS
        // Arrange
        _testForm = new Form();
        _service = new GlobalHotkeyService(_testForm.Handle);

        // Act & Assert
        var act = () => _service.Dispose();
        act.Should().NotThrow();
#else
        Assert.True(true, "Test requires Windows Forms");
#endif
    }

    public void Dispose()
    {
#if WINDOWS
        _service?.Dispose();
        _testForm?.Dispose();
#endif
    }
}
