using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MpvNet.Windows.WinForms.Controls;

// Volumen como el de VLC: una cuña que sube hacia la derecha, verde hasta el 100% y
// de ahí al rojo hasta 125%. Se arrastra, se hace clic o se le da a la rueda encima.
public class VolumeWedge : Control
{
    // mpv deja subir hasta volume-max (130 por defecto); 125 es el tope de VLC
    public const int MaxVolume = 125;

    int _volume = 100;
    bool _muted;
    bool _isDragging;

    public event Action<int>? VolumeChanged;

    public VolumeWedge()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        Cursor = Cursors.Hand;
        ForeColor = Color.White;
    }

    // lo llama el timer de la barra con lo que diga mpv; mientras se arrastra manda el ratón
    public void Show(int volume, bool muted)
    {
        if (_isDragging)
            return;

        volume = Math.Clamp(volume, 0, MaxVolume);

        if (volume == _volume && muted == _muted)
            return;

        _volume = volume;
        _muted = muted;
        Invalidate();
    }

    void SetFromUser(int volume)
    {
        volume = Math.Clamp(volume, 0, MaxVolume);

        if (volume == _volume)
            return;

        _volume = volume;
        Invalidate();
        VolumeChanged?.Invoke(volume);
    }

    int VolumeAt(int x) => (int)Math.Round((x - 1) * (double)MaxVolume / Math.Max(1, Width - 3));

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button != MouseButtons.Left)
            return;

        _isDragging = true;
        SetFromUser(VolumeAt(e.X));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isDragging)
            SetFromUser(VolumeAt(e.X));
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _isDragging = false;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        SetFromUser(_volume + (e.Delta > 0 ? 5 : -5));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // arriba queda sitio para el porcentaje, la cuña ocupa el resto
        int textHeight = Font.Height;
        RectangleF area = new RectangleF(1, textHeight, Width - 3, Height - textHeight - 2);

        if (area.Width <= 0 || area.Height <= 0)
            return;

        using GraphicsPath wedge = new GraphicsPath();
        wedge.AddPolygon(new[]
        {
            new PointF(area.Left, area.Bottom),
            new PointF(area.Right, area.Bottom),
            new PointF(area.Right, area.Top)
        });

        using (SolidBrush empty = new SolidBrush(Color.FromArgb(60, 60, 60)))
            g.FillPath(empty, wedge);

        float filled = area.Width * _volume / MaxVolume;

        if (filled > 0)
        {
            g.SetClip(wedge);

            if (_muted)
            {
                using SolidBrush grey = new SolidBrush(Color.FromArgb(120, 120, 120));
                g.FillRectangle(grey, area.Left, area.Top, filled, area.Height);
            }
            else
            {
                using LinearGradientBrush level = new LinearGradientBrush(area, Color.Green, Color.Red, LinearGradientMode.Horizontal);

                // 0.8 del ancho es el 100% de volumen: hasta ahí verde, luego avisa
                level.InterpolationColors = new ColorBlend
                {
                    Positions = new[] { 0f, 0.6f, 0.8f, 1f },
                    Colors = new[]
                    {
                        Color.FromArgb(60, 170, 70),
                        Color.FromArgb(110, 200, 70),
                        Color.FromArgb(235, 200, 50),
                        Color.FromArgb(225, 60, 45)
                    }
                };

                g.FillRectangle(level, area.Left, area.Top, filled, area.Height);
            }

            g.ResetClip();
        }

        using (Pen outline = new Pen(Color.FromArgb(110, 110, 110)))
        {
            g.DrawPath(outline, wedge);

            // marquita del 100%
            float x100 = area.Left + area.Width * 100 / MaxVolume;
            g.DrawLine(outline, x100, area.Bottom, x100, area.Bottom - 4);
        }

        Color textColor = _muted ? Color.Gray : _volume > 100 ? Color.FromArgb(240, 120, 100) : ForeColor;

        TextRenderer.DrawText(g, _muted ? "mute" : _volume + "%", Font,
            new Rectangle(0, 0, Width, textHeight), textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
