using System.Runtime.InteropServices;

namespace OpenAIDictate.Services;

/// <summary>
/// Registers and handles global hotkeys using Windows API
/// Default hotkey: F5 (no modifiers)
/// </summary>
public class GlobalHotkeyService : IDisposable
{
    // Windows API constants
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 1;

    // Virtual key codes
    private const int VK_F5 = 0x74;

    // Hotkey modifiers
    [Flags]
    private enum KeyModifier
    {
        None = 0x0000,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008
    }

    // Windows API imports
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly IntPtr _windowHandle;
    private bool _isRegistered;
    private uint _currentModifiers;
    private uint _currentVirtualKey;
    private string _currentGesture = "F5";

    public event EventHandler? HotkeyPressed;

    public GlobalHotkeyService(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    /// <summary>
    /// Registers a hotkey globally using a gesture string (e.g., "F5", "Ctrl+F6")
    /// </summary>
    public bool Register(string gesture = "F5")
    {
        if (_isRegistered)
        {
            Logger.LogWarning("Hotkey already registered. Unregister first.");
            return true;
        }

        try
        {
            // Parse gesture
            var (modifiers, virtualKey) = HotkeyParser.Parse(gesture);
            _currentModifiers = modifiers;
            _currentVirtualKey = virtualKey;
            _currentGesture = gesture;

            // Register with Windows
            _isRegistered = RegisterHotKey(
                _windowHandle,
                HOTKEY_ID,
                _currentModifiers,
                _currentVirtualKey
            );

            if (_isRegistered)
            {
                Logger.LogInfo($"Hotkey '{gesture}' registered successfully (VK: 0x{virtualKey:X}, Mods: {modifiers})");
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                Logger.LogError($"Failed to register hotkey '{gesture}'. Error code: {error}");

                throw new InvalidOperationException(
                    $"Failed to register hotkey '{gesture}'. It may already be in use by another application. " +
                    "Please try a different hotkey combination."
                );
            }

            return _isRegistered;
        }
        catch (ArgumentException ex)
        {
            Logger.LogError($"Invalid hotkey gesture '{gesture}': {ex.Message}");
            throw new InvalidOperationException($"Invalid hotkey gesture: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error registering hotkey: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Changes the registered hotkey to a new gesture
    /// </summary>
    public bool ChangeHotkey(string newGesture)
    {
        Unregister();
        return Register(newGesture);
    }

    /// <summary>
    /// Gets the current hotkey gesture string
    /// </summary>
    public string GetCurrentGesture() => _currentGesture;

    /// <summary>
    /// Unregisters the hotkey
    /// </summary>
    public void Unregister()
    {
        if (!_isRegistered)
            return;

        try
        {
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            _isRegistered = false;
            Logger.LogInfo($"Hotkey '{_currentGesture}' unregistered");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error unregistering hotkey: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes Windows messages to detect hotkey press
    /// Call this from your message loop (WndProc)
    /// </summary>
    public bool ProcessMessage(int msg, IntPtr wParam)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            Logger.LogInfo($"Hotkey '{_currentGesture}' pressed");
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        Unregister();
        GC.SuppressFinalize(this);
    }
}
