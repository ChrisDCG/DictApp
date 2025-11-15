using System.Runtime.InteropServices;

namespace OpenAIDictate.Services;

/// <summary>
/// Injects text at the current cursor position using clipboard + Ctrl+V
/// Works universally with Outlook, Word, Notepad, and most Windows applications
/// </summary>
public static class TextInjector
{
    // Windows API for SendInput
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // Input type
    private const uint INPUT_KEYBOARD = 1;

    // Key event flags
    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    // Virtual key codes
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;

    /// <summary>
    /// Injects text at the current cursor position
    /// Uses clipboard + Ctrl+V simulation (universally compatible)
    /// </summary>
    public static async Task InjectAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Logger.LogWarning("Attempted to inject empty text");
            return;
        }

        Logger.LogInfo($"Injecting text ({text.Length} characters)");

        try
        {
            // 1. Backup current clipboard content
            IDataObject? originalClipboard = null;
            try
            {
                if (Clipboard.ContainsText() || Clipboard.ContainsImage() || Clipboard.ContainsFileDropList())
                {
                    originalClipboard = Clipboard.GetDataObject();
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to backup clipboard: {ex.Message}");
            }

            // 2. Set transcription text to clipboard (with retry logic)
            bool clipboardSet = false;
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    Clipboard.SetText(text);
                    clipboardSet = true;
                    break;
                }
                catch (Exception ex)
                {
                    if (attempt == 3)
                    {
                        Logger.LogError($"Failed to set clipboard after 3 attempts: {ex.Message}");
                        throw new InvalidOperationException("Failed to access clipboard. It may be locked by another application.", ex);
                    }
                    Logger.LogWarning($"Clipboard locked, retrying ({attempt}/3)...");
                    await Task.Delay(50);
                }
            }

            if (!clipboardSet)
                return;

            // 3. Small delay to ensure clipboard is set
            await Task.Delay(30);

            // 4. Simulate Ctrl+V keystroke
            SendCtrlV();

            // 5. Small delay before restoring clipboard
            await Task.Delay(100);

            // 6. Restore original clipboard content (best effort)
            if (originalClipboard != null)
            {
                try
                {
                    Clipboard.SetDataObject(originalClipboard, true);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Failed to restore clipboard: {ex.Message}");
                    // Not critical - don't throw
                }
            }

            Logger.LogInfo("Text injection completed");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error during text injection: {ex.Message}");
            throw new InvalidOperationException($"Failed to inject text: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Simulates Ctrl+V keystroke using SendInput
    /// </summary>
    private static void SendCtrlV()
    {
        var inputs = new INPUT[4];

        // Ctrl down
        inputs[0] = CreateKeyInput(VK_CONTROL, KEYEVENTF_KEYDOWN);

        // V down
        inputs[1] = CreateKeyInput(VK_V, KEYEVENTF_KEYDOWN);

        // V up
        inputs[2] = CreateKeyInput(VK_V, KEYEVENTF_KEYUP);

        // Ctrl up
        inputs[3] = CreateKeyInput(VK_CONTROL, KEYEVENTF_KEYUP);

        // Send input
        uint result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

        if (result != inputs.Length)
        {
            Logger.LogError($"SendInput failed. Expected {inputs.Length}, sent {result}");
            throw new InvalidOperationException("Failed to send Ctrl+V keystroke");
        }
    }

    /// <summary>
    /// Creates a keyboard input structure
    /// </summary>
    private static INPUT CreateKeyInput(ushort virtualKey, uint flags)
    {
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            union = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }
}
