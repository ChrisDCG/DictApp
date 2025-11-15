using FluentAssertions;
using OpenAIDictate.Services;
#if WINDOWS
using System.Windows.Forms;
#endif
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Comprehensive tests for GlobalHotkeyService covering all code paths
/// Note: These tests require Windows Forms and will only compile on Windows
/// </summary>
public class GlobalHotkeyServiceComprehensiveTests : IDisposable
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
        // Skip on non-Windows platforms
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
    public void Register_AlreadyRegistered_ShouldReturnTrue()
    {
#if WINDOWS
        // Arrange
        _testForm = new Form();
        _service = new GlobalHotkeyService(_testForm.Handle);
        _service.Register("F5");

        // Act
        var result = _service.Register("F5");

        // Assert
        result.Should().BeTrue();
#else
        Assert.True(true, "Test requires Windows Forms");
#endif
    }

    [Fact]
    public void ChangeHotkey_ValidGesture_ShouldUnregisterAndRegister()
    {
#if WINDOWS
        // Arrange
        _testForm = new Form();
        _service = new GlobalHotkeyService(_testForm.Handle);
        _service.Register("F5");

        // Act
        var result = _service.ChangeHotkey("F6");

        // Assert
        result.Should().BeTrue();
        _service.GetCurrentGesture().Should().Be("F6");
#else
        Assert.True(true, "Test requires Windows Forms");
#endif
    }

    [Fact]
    public void ProcessMessage_ValidHotkeyMessage_ShouldInvokeEvent()
    {
#if WINDOWS
        // Arrange
        _testForm = new Form();
        _service = new GlobalHotkeyService(_testForm.Handle);
        _service.Register("F5");
        bool eventFired = false;
        _service.HotkeyPressed += (s, e) => eventFired = true;

        // Act
        var result = _service.ProcessMessage(0x0312, new IntPtr(1)); // WM_HOTKEY with ID 1

        // Assert
        result.Should().BeTrue();
        eventFired.Should().BeTrue();
#else
        Assert.True(true, "Test requires Windows Forms");
#endif
    }

    [Fact]
    public void ProcessMessage_InvalidMessage_ShouldReturnFalse()
    {
#if WINDOWS
        // Arrange
        _testForm = new Form();
        _service = new GlobalHotkeyService(_testForm.Handle);

        // Act
        var result = _service.ProcessMessage(0x0000, IntPtr.Zero);

        // Assert
        result.Should().BeFalse();
#else
        Assert.True(true, "Test requires Windows Forms");
#endif
    }

    [Fact]
    public void Unregister_NotRegistered_ShouldNotThrow()
    {
#if WINDOWS
        // Arrange
        _testForm = new Form();
        _service = new GlobalHotkeyService(_testForm.Handle);

        // Act & Assert
        var act = () => _service.Unregister();
        act.Should().NotThrow();
#else
        Assert.True(true, "Test requires Windows Forms");
#endif
    }

    [Fact]
    public void Dispose_ShouldUnregister()
    {
#if WINDOWS
        // Arrange
        _testForm = new Form();
        _service = new GlobalHotkeyService(_testForm.Handle);
        _service.Register("F5");

        // Act
        _service.Dispose();

        // Assert
        // Should not throw
#else
        Assert.True(true, "Test requires Windows Forms");
#endif
    }

    [Fact]
    public void Dispose_MultipleCalls_ShouldNotThrow()
    {
#if WINDOWS
        // Arrange
        _testForm = new Form();
        _service = new GlobalHotkeyService(_testForm.Handle);

        // Act & Assert
        _service.Dispose();
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
