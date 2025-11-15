namespace OpenAIDictate.Services;

/// <summary>
/// Parses hotkey gestures and converts them to Windows API virtual key codes and modifiers
/// </summary>
public static class HotkeyParser
{
    [Flags]
    public enum KeyModifier
    {
        None = 0x0000,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008
    }

    // Virtual key codes for function keys
    private static readonly Dictionary<string, int> VirtualKeyCodes = new()
    {
        { "F1", 0x70 }, { "F2", 0x71 }, { "F3", 0x72 }, { "F4", 0x73 },
        { "F5", 0x74 }, { "F6", 0x75 }, { "F7", 0x76 }, { "F8", 0x77 },
        { "F9", 0x78 }, { "F10", 0x79 }, { "F11", 0x7A }, { "F12", 0x7B },
        { "A", 0x41 }, { "B", 0x42 }, { "C", 0x43 }, { "D", 0x44 },
        { "E", 0x45 }, { "F", 0x46 }, { "G", 0x47 }, { "H", 0x48 },
        { "I", 0x49 }, { "J", 0x4A }, { "K", 0x4B }, { "L", 0x4C },
        { "M", 0x4D }, { "N", 0x4E }, { "O", 0x4F }, { "P", 0x50 },
        { "Q", 0x51 }, { "R", 0x52 }, { "S", 0x53 }, { "T", 0x54 },
        { "U", 0x55 }, { "V", 0x56 }, { "W", 0x57 }, { "X", 0x58 },
        { "Y", 0x59 }, { "Z", 0x5A },
        { "0", 0x30 }, { "1", 0x31 }, { "2", 0x32 }, { "3", 0x33 },
        { "4", 0x34 }, { "5", 0x35 }, { "6", 0x36 }, { "7", 0x37 },
        { "8", 0x38 }, { "9", 0x39 }
    };

    /// <summary>
    /// Parses a hotkey gesture string (e.g., "Ctrl+F5", "Alt+Shift+A", "F12")
    /// Returns (modifiers, virtualKeyCode)
    /// </summary>
    public static (uint Modifiers, uint VirtualKey) Parse(string gesture)
    {
        if (string.IsNullOrWhiteSpace(gesture))
        {
            throw new ArgumentException("Hotkey gesture cannot be empty", nameof(gesture));
        }

        var parts = gesture.Split('+').Select(p => p.Trim().ToUpper()).ToArray();

        uint modifiers = 0;
        string? keyPart = null;

        foreach (var part in parts)
        {
            switch (part)
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= (uint)KeyModifier.Control;
                    break;

                case "ALT":
                    modifiers |= (uint)KeyModifier.Alt;
                    break;

                case "SHIFT":
                    modifiers |= (uint)KeyModifier.Shift;
                    break;

                case "WIN":
                case "WINDOWS":
                    modifiers |= (uint)KeyModifier.Win;
                    break;

                default:
                    // This should be the actual key
                    if (keyPart != null)
                    {
                        throw new ArgumentException($"Invalid hotkey gesture: '{gesture}'. Multiple keys specified.", nameof(gesture));
                    }
                    keyPart = part;
                    break;
            }
        }

        if (string.IsNullOrEmpty(keyPart))
        {
            throw new ArgumentException($"Invalid hotkey gesture: '{gesture}'. No key specified.", nameof(gesture));
        }

        if (!VirtualKeyCodes.TryGetValue(keyPart, out int virtualKey))
        {
            throw new ArgumentException($"Unsupported key: '{keyPart}'. Supported: F1-F12, A-Z, 0-9.", nameof(gesture));
        }

        return (modifiers, (uint)virtualKey);
    }

    /// <summary>
    /// Formats modifiers and virtual key back to a readable gesture string
    /// </summary>
    public static string Format(uint modifiers, uint virtualKey)
    {
        var parts = new List<string>();

        if ((modifiers & (uint)KeyModifier.Control) != 0)
            parts.Add("Ctrl");

        if ((modifiers & (uint)KeyModifier.Alt) != 0)
            parts.Add("Alt");

        if ((modifiers & (uint)KeyModifier.Shift) != 0)
            parts.Add("Shift");

        if ((modifiers & (uint)KeyModifier.Win) != 0)
            parts.Add("Win");

        // Find key name
        var keyName = VirtualKeyCodes.FirstOrDefault(kvp => kvp.Value == virtualKey).Key ?? $"0x{virtualKey:X}";
        parts.Add(keyName);

        return string.Join("+", parts);
    }

    /// <summary>
    /// Validates if a hotkey gesture string is valid
    /// </summary>
    public static bool IsValid(string gesture)
    {
        try
        {
            Parse(gesture);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a list of suggested hotkey gestures for UI
    /// </summary>
    public static List<string> GetSuggestions()
    {
        return new List<string>
        {
            "F5",
            "F6",
            "F7",
            "F8",
            "F9",
            "F10",
            "F11",
            "F12",
            "Ctrl+F5",
            "Ctrl+F6",
            "Ctrl+Shift+F5",
            "Alt+F5",
            "Ctrl+Alt+R"
        };
    }
}
