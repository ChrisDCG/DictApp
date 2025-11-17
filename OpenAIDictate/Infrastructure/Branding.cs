using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace OpenAIDictate.Infrastructure;

/// <summary>
/// Provides branding assets generated at runtime (icon, colors, etc.)
/// Avoids shipping static binary resources while ensuring a unique visual identity.
/// </summary>
public static class Branding
{
    private static readonly Lazy<Icon> _appIcon = new(CreateTriangleIcon);

    /// <summary>
    /// Primary application icon used for tray and dialogs.
    /// </summary>
    public static Icon AppIcon => _appIcon.Value;

    private static Icon CreateTriangleIcon()
    {
        const int size = 64;
        using var bmp = new Bitmap(size, size);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var backgroundBrush = new SolidBrush(Color.FromArgb(255, 15, 18, 33));
            g.FillEllipse(backgroundBrush, 0, 0, size, size);

            PointF[] triangle =
            {
                new(size * 0.32f, size * 0.26f),
                new(size * 0.72f, size * 0.50f),
                new(size * 0.32f, size * 0.74f)
            };

            using var triangleBrush = new LinearGradientBrush(
                new PointF(0, 0),
                new PointF(size, size),
                Color.FromArgb(255, 0, 196, 255),
                Color.FromArgb(255, 0, 120, 215));

            g.FillPolygon(triangleBrush, triangle);
        }

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            // Clone to detach from unmanaged handle
            return (Icon)Icon.FromHandle(hIcon).Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
