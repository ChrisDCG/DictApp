using System.Drawing;
using System.Windows.Forms;

namespace OpenAIDictate.Infrastructure;

/// <summary>
/// Displays a subtle pulsing indicator near the caret while recording/transcribing.
/// </summary>
public sealed class CursorPulseOverlay : Form
{
    private readonly System.Windows.Forms.Timer _animationTimer;
    private float _phase;
    private const int OverlaySize = 32;

    public CursorPulseOverlay()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Enabled = false;
        Width = OverlaySize;
        Height = OverlaySize;
        DoubleBuffered = true;
        BackColor = Color.Lime;
        TransparencyKey = Color.Lime;
        Opacity = 0;

        _animationTimer = new System.Windows.Forms.Timer
        {
            Interval = 30
        };
        _animationTimer.Tick += (_, _) => Animate();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_LAYERED = 0x00080000;
            const int WS_EX_TRANSPARENT = 0x00000020;

            CreateParams cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_LAYERED | WS_EX_TRANSPARENT;
            return cp;
        }
    }

    private void Animate()
    {
        _phase += 0.12f;
        if (_phase > MathF.PI * 2)
        {
            _phase -= MathF.PI * 2;
        }

        float opacity = 0.45f + (0.25f * (float)Math.Sin(_phase));
        Opacity = opacity;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        float pulse = 6f * (float)(1 + Math.Sin(_phase));
        float radius = (OverlaySize / 2f) - 4f;
        float innerRadius = radius - pulse;
        var center = new PointF(OverlaySize / 2f, OverlaySize / 2f);

        using var gradientBrush = new System.Drawing.Drawing2D.PathGradientBrush(new[]
        {
            new PointF(center.X - radius, center.Y),
            new PointF(center.X, center.Y - radius),
            new PointF(center.X + radius, center.Y),
            new PointF(center.X, center.Y + radius)
        })
        {
            CenterColor = Color.FromArgb(180, 0, 196, 255),
            SurroundColors = new[]
            {
                Color.FromArgb(30, 0, 196, 255),
                Color.FromArgb(30, 0, 196, 255),
                Color.FromArgb(30, 0, 196, 255),
                Color.FromArgb(30, 0, 196, 255)
            }
        };

        e.Graphics.FillEllipse(gradientBrush, center.X - radius, center.Y - radius, radius * 2, radius * 2);

        using var innerBrush = new SolidBrush(Color.FromArgb(220, 0, 120, 215));
        e.Graphics.FillEllipse(innerBrush, center.X - innerRadius, center.Y - innerRadius, innerRadius * 2, innerRadius * 2);
    }

    public void ShowAtCaret()
    {
        RunOnUiThread(() =>
        {
            PositionOverlay();
            if (!Visible)
            {
                Show();
            }
            _animationTimer.Start();
        });
    }

    public void UpdatePosition()
    {
        RunOnUiThread(PositionOverlay);
    }

    public void HideOverlay()
    {
        RunOnUiThread(() =>
        {
            _animationTimer.Stop();
            Hide();
        });
    }

    private void PositionOverlay()
    {
        Point location = GetTargetPosition();
        Location = new Point(location.X - Width / 2, location.Y - Height / 2 - 18);
    }

    private static Point GetTargetPosition()
    {
        Point? caret = CaretLocator.TryGetCaretScreenPosition();
        return caret ?? Cursor.Position;
    }

    private void RunOnUiThread(Action action)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(action);
            }
            catch (ObjectDisposedException)
            {
                // Ignore - shutting down
            }
        }
        else
        {
            action();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer.Stop();
            _animationTimer.Dispose();
        }

        base.Dispose(disposing);
    }
}
