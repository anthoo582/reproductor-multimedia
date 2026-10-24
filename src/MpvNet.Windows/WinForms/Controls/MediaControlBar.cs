using System.Drawing;
using System.Windows.Forms;
using MpvNet;

namespace MpvNet.Windows.WinForms.Controls;

// Base de las tres barras: alto fijo en píxeles lógicos escalado por DPI, fondo y
// estilo de botón compartidos. El contenido se adapta a la ventana, la barra nunca.
public abstract class MediaControlBar : Panel
{
    public const int LogicalHeight = 60;
    const int LogicalMargin = 8;
    const int LogicalButtonSize = 48;

    int _cursorX;
    int _cursorY = 10;

    protected MediaControlBar()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(32, 32, 32);
        Font = new Font("Segoe UI", 9f);

    }

    // en WinForms el panel ya viene escalado por AutoScaleMode.Dpi, así que las
    // medidas lógicas se multiplican por la escala actual del control
    int S(int logical) => (int)Math.Round(logical * (DeviceDpi / 96f));

    protected Button AddButton(string text, int width = LogicalButtonSize)
    {
        Button btn = new Button
        {
            Text = text,
            Left = _cursorX,
            Top = _cursorY,
            Width = S(width),
            Height = S(LogicalButtonSize),
            Font = new Font("Segoe UI", 12f),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };

        btn.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(90, 90, 90);

        Controls.Add(btn);
        Advance(width, LogicalMargin);
        return btn;
    }

    protected void AddGap(int logical = LogicalMargin) => Advance(0, logical);

    void Advance(int width, int gap)
    {
        _cursorX += S(width) + S(gap);
    }

    protected TrackBar AddTimeline(int logicalWidth)
    {
        TrackBar track = new TrackBar
        {
            Left = _cursorX,
            Top = S(15),
            Width = S(logicalWidth),
            Height = S(30),
            Minimum = 0,
            Maximum = 1000,
            TickStyle = TickStyle.None,
            BackColor = BackColor
        };

        Controls.Add(track);
        Advance(logicalWidth, LogicalMargin);
        return track;
    }

    protected VolumeWedge AddVolumeWedge(int logicalWidth)
    {
        VolumeWedge wedge = new VolumeWedge
        {
            Left = _cursorX,
            Top = S(10),
            Width = S(logicalWidth),
            Height = S(42),
            Font = new Font("Segoe UI", 8f),
            BackColor = BackColor
        };

        Controls.Add(wedge);
        Advance(logicalWidth, LogicalMargin);
        return wedge;
    }

    protected ComboBox AddSpeedSelector()
    {
        ComboBox cmb = new ComboBox
        {
            Left = _cursorX,
            Top = S(15),
            Width = S(64),
            Height = S(30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        cmb.Items.AddRange(new object[] { "0.25x", "0.5x", "0.75x", "1x", "1.25x", "1.5x", "2x" });
        cmb.SelectedIndex = 3;

        Controls.Add(cmb);
        Advance(64, LogicalMargin);
        return cmb;
    }

    protected Label AddInfoLabel(int logicalWidth, ContentAlignment align)
    {
        Label lbl = new Label
        {
            Left = _cursorX,
            Top = S(20),
            Width = S(logicalWidth),
            Height = S(24),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = align,
            AutoEllipsis = true
        };

        Controls.Add(lbl);
        Advance(logicalWidth, 0);
        return lbl;
    }

    // la etiqueta de info se estira hasta el borde derecho de la barra
    protected void StretchInfoLabel(Label lbl)
    {
        void Relayout()
        {
            int right = ClientSize.Width - S(LogicalMargin);
            lbl.Width = Math.Max(S(80), right - lbl.Left);
        }

        Resize += (s, e) => Relayout();
        Relayout();
    }

    protected static string FormatTime(double seconds)
    {
        if (double.IsNaN(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)
            return "00:00";

        TimeSpan time = TimeSpan.FromSeconds(seconds);

        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{time.Minutes:00}:{time.Seconds:00}";
    }

    // posición 0..1 -> segundos absolutos, sin tocar nada si no hay duración
    protected static void SeekToFraction(double fraction)
    {
        double duration = Player.GetPropertyDouble("duration");

        if (duration > 0)
            Player.SetPropertyDouble("time-pos", duration * fraction);
    }
}
