using System.Drawing;
using System.Runtime.InteropServices;

namespace OpenAIDictate.Infrastructure;

/// <summary>
/// Provides best-effort caret and cursor location detection for overlay positioning.
/// </summary>
public static class CaretLocator
{
    public static Point? TryGetCaretScreenPosition()
    {
        IntPtr foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            return null;
        }

        uint targetThreadId = GetWindowThreadProcessId(foreground, out _);
        uint currentThreadId = GetCurrentThreadId();
        bool attached = false;

        try
        {
            if (targetThreadId != currentThreadId)
            {
                attached = AttachThreadInput(currentThreadId, targetThreadId, true);
            }

            IntPtr focus = GetFocus();
            if (focus == IntPtr.Zero)
            {
                return null;
            }

            if (!GetCaretPos(out POINT caret))
            {
                return null;
            }

            if (!ClientToScreen(focus, ref caret))
            {
                return null;
            }

            return new Point(caret.X, caret.Y);
        }
        finally
        {
            if (attached)
            {
                AttachThreadInput(currentThreadId, targetThreadId, false);
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetFocus();

    [DllImport("user32.dll")]
    private static extern bool GetCaretPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}
