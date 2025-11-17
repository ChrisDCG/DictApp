using System.Drawing;
using System.Windows.Forms;

namespace OpenAIDictate.Infrastructure;

/// <summary>
/// Displays a subtle pulsing indicator near the caret while recording/transcribing.
/// </summary>
public sealed class CursorPulseOverlay : Form
{
    private readonly System.Windows.Forms.Timer _animationTimer;
    private readonly System.Windows.Forms.Timer _positionTimer;
    private float _phase;
    private const int OverlaySize = 56;
    private OverlayMode _mode = OverlayMode.Recording;

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

        _positionTimer = new System.Windows.Forms.Timer
        {
            Interval = 120
        };
        _positionTimer.Tick += (_, _) => PositionOverlay();
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_LAYERED = 0x00080000;
            const int WS_EX_TRANSPARENT = 0x00000020;
            const int WS_EX_NOACTIVATE = 0x08000000;

            CreateParams cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    private void Animate()
    {
        float speed = _mode == OverlayMode.Recording ? 0.2f : 0.08f;
        _phase += speed;
        if (_phase > MathF.PI * 2)
        {
            _phase -= MathF.PI * 2;
        }

        float baseOpacity = _mode == OverlayMode.Recording ? 0.8f : 0.55f;
        float amplitude = _mode == OverlayMode.Recording ? 0.35f : 0.2f;
        float opacity = baseOpacity + (amplitude * (float)Math.Sin(_phase));
        Opacity = opacity;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        float radius = (OverlaySize / 2f) - 4f;
        float ringWidth = (_mode == OverlayMode.Recording ? 7f : 4f) + (2f * (float)(Math.Sin(_phase * 1.5f) + 1));
        var center = new PointF(OverlaySize / 2f, OverlaySize / 2f);

        Color edgeColor = _mode == OverlayMode.Recording ? Color.FromArgb(150, 0, 196, 255) : Color.FromArgb(90, 0, 196, 255);
        using var outerPen = new Pen(edgeColor, ringWidth);
        e.Graphics.DrawEllipse(outerPen, center.X - radius, center.Y - radius, radius * 2, radius * 2);

        using var fillBrush = new SolidBrush(_mode == OverlayMode.Recording ? Color.FromArgb(210, 0, 120, 215) : Color.FromArgb(160, 0, 140, 220));
        float innerRadius = radius - 8;
        e.Graphics.FillEllipse(fillBrush, center.X - innerRadius, center.Y - innerRadius, innerRadius * 2, innerRadius * 2);

        using var highlightBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255));
        float highlightRadius = innerRadius / 2.5f;
        e.Graphics.FillEllipse(highlightBrush, center.X - highlightRadius, center.Y - highlightRadius, highlightRadius * 2, highlightRadius * 2);
    }

    public void ShowOverlay(OverlayMode mode)
    {
        RunOnUiThread(() =>
        {
            _mode = mode;
            PositionOverlay();
            if (!Visible)
            {
                Show();
            }
            Opacity = 0.8;
            _positionTimer.Start();
            _animationTimer.Start();
        });
    }

    public void TransitionToMode(OverlayMode mode)
    {
        RunOnUiThread(() =>
        {
            _mode = mode;
            PositionOverlay();
        });
    }

    public void HideOverlay()
    {
        RunOnUiThread(() =>
        {
            _animationTimer.Stop();
            _positionTimer.Stop();
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
            _positionTimer.Stop();
            _positionTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    public enum OverlayMode
    {
        Recording,
        Transcribing
    }
}
