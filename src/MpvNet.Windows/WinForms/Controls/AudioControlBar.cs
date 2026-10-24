using System.Drawing;
using System.Windows.Forms;
using MpvNet;

namespace MpvNet.Windows.WinForms.Controls;

public class AudioControlBar : MediaControlBar
{
    readonly Button _btnShuffle;
    readonly Button _btnPrevious;
    readonly Button _btnPlayPause;
    readonly Button _btnStop;
    readonly Button _btnNext;
    readonly Button _btnRepeat;
    readonly TrackBar _trackTimeline;
    readonly VolumeWedge _volumeWedge;
    readonly Button _btnMute;
    readonly Button _btnPlaylist;
    readonly Label _lblTime;
    readonly Label _lblDuration;
    readonly Label _lblInfo;

    bool _isDraggingTimeline;
    bool _isSyncing;
    int _tick;

    public event Action? PreviousClicked;
    public event Action? PlayPauseClicked;
    public event Action? StopClicked;
    public event Action? NextClicked;
    public event Action<double>? TimelineChanged;
    public event Action<int>? VolumeChanged;
    public event Action? MuteClicked;
    public event Action<bool>? ShuffleChanged;
    public event Action<int>? RepeatChanged;   // 0 off, 1 all, 2 one
    public event Action? PlaylistClicked;

    public AudioControlBar()
    {
        _btnShuffle = AddButton("\uD83D\uDD00");
        _btnShuffle.Click += (s, e) =>
        {
            bool enabled = !Player.GetPropertyBool("playlist-shuffle");
            SetShuffleState(enabled);
            ShuffleChanged?.Invoke(enabled);
        };

        _btnPrevious = AddButton("\u23EE");
        _btnPrevious.Click += (s, e) => PreviousClicked?.Invoke();

        _btnPlayPause = AddButton("\u25B6");
        _btnPlayPause.Click += (s, e) => PlayPauseClicked?.Invoke();

        _btnStop = AddButton("\u25A0");
        _btnStop.Click += (s, e) => StopClicked?.Invoke();

        _btnNext = AddButton("\u23ED");
        _btnNext.Click += (s, e) => NextClicked?.Invoke();

        _btnRepeat = AddButton("\uD83D\uDD01");
        _btnRepeat.Click += (s, e) =>
        {
            int mode = (GetRepeatMode() + 1) % 3;
            SetRepeatState(mode);
            RepeatChanged?.Invoke(mode);
        };

        AddGap(12);

        _lblTime = AddInfoLabel(56, ContentAlignment.MiddleCenter);

        _trackTimeline = AddTimeline(320);
        _trackTimeline.MouseDown += (s, e) =>
        {
            _isDraggingTimeline = true;
            TimelineChanged?.Invoke(_trackTimeline.Value / 1000.0);
        };
        _trackTimeline.MouseUp += (s, e) =>
        {
            _isDraggingTimeline = false;
            TimelineChanged?.Invoke(_trackTimeline.Value / 1000.0);
        };

        _lblDuration = AddInfoLabel(56, ContentAlignment.MiddleCenter);

        AddGap(12);

        _btnMute = AddButton("\uD83D\uDD0A");
        _btnMute.Click += (s, e) => MuteClicked?.Invoke();

        _volumeWedge = AddVolumeWedge(100);
        _volumeWedge.VolumeChanged += volume => VolumeChanged?.Invoke(volume);

        AddGap(12);

        _btnPlaylist = AddButton("\u2261");
        _btnPlaylist.Click += (s, e) => PlaylistClicked?.Invoke();

        _lblInfo = AddInfoLabel(200, ContentAlignment.MiddleLeft);
        StretchInfoLabel(_lblInfo);
    }

    void SetShuffleState(bool enabled) =>
        _btnShuffle.BackColor = enabled ? Color.FromArgb(0, 120, 215) : Color.FromArgb(50, 50, 50);

    void SetRepeatState(int mode)
    {
        _btnRepeat.Text = mode == 2 ? "\uD83D\uDD02" : "\uD83D\uDD01";
        _btnRepeat.BackColor = mode > 0 ? Color.FromArgb(0, 120, 215) : Color.FromArgb(50, 50, 50);
    }

    static int GetRepeatMode()
    {
        if (Player.GetPropertyString("loop-file") == "inf")
            return 2;

        return Player.GetPropertyString("loop-playlist") == "inf" ? 1 : 0;
    }

    public void UpdateUI()
    {
        bool slow = (++_tick & 3) == 0;

        try
        {
            _btnPlayPause.Text = Player.Paused ? "\u25B6" : "\u23F8";

            if (slow)
            {
                int playlistCount = Player.GetPropertyInt("playlist-count");
                _btnPrevious.Enabled = playlistCount > 1 && Player.PlaylistPos > 0;
                _btnNext.Enabled = playlistCount > 1 && Player.PlaylistPos < playlistCount - 1;

                if (!_isSyncing)
                {
                    SetShuffleState(Player.GetPropertyBool("playlist-shuffle"));
                    SetRepeatState(GetRepeatMode());
                }

                string path = Player.Path;

                if (string.IsNullOrEmpty(path))
                {
                    _lblInfo.Text = "";
                    return;
                }

                // el título de la pista suele venir en metadata de la etiqueta
                string title = Player.GetPropertyString("media-title");
                string name = string.IsNullOrEmpty(title) ? Path.GetFileNameWithoutExtension(path) : title;
                string positionLabel = playlistCount > 0 ? $"{Player.PlaylistPos + 1}/{playlistCount} \u00B7 " : "";

                _lblInfo.Text = positionLabel + name;
            }

            double duration = Player.Duration.TotalSeconds;
            double position = Player.GetPropertyDouble("time-pos", false);

            if (!_isDraggingTimeline)
            {
                if (duration > 0)
                    _trackTimeline.Value = Math.Clamp((int)(position / duration * 1000), 0, 1000);

                _lblTime.Text = FormatTime(position);
                _lblDuration.Text = FormatTime(duration);
            }

            bool muted = Player.GetPropertyBool("mute");
            _volumeWedge.Show(Player.GetPropertyInt("volume"), muted);
            _btnMute.Text = muted ? "🔇" : "🔊";
        }
        catch
        {
        }
    }

    // tras cambiar desde C# esperamos al siguiente UpdateUI para no pelear con el bool
    public void MarkSynced() => _isSyncing = true;

    public void ClearSynced() => _isSyncing = false;
}
