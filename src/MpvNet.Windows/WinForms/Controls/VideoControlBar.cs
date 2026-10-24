using System.Drawing;
using System.Windows.Forms;
using MpvNet;

namespace MpvNet.Windows.WinForms.Controls;

public class VideoControlBar : MediaControlBar
{
    readonly Button _btnPrevious;
    readonly Button _btnPlayPause;
    readonly Button _btnStop;
    readonly Button _btnNext;
    readonly TrackBar _trackTimeline;
    readonly VolumeWedge _volumeWedge;
    readonly Button _btnMute;
    readonly ComboBox _cmbSpeed;
    readonly Button _btnFullscreen;
    readonly Button _btnPlaylist;
    readonly Label _lblTime;
    readonly Label _lblDuration;
    readonly Label _lblInfo;

    bool _isDraggingTimeline;
    bool _isUpdatingUI;
    int _tick;

    public event Action? PreviousClicked;
    public event Action? PlayPauseClicked;
    public event Action? StopClicked;
    public event Action? NextClicked;
    public event Action<double>? TimelineChanged;
    public event Action<int>? VolumeChanged;
    public event Action? MuteClicked;
    public event Action<double>? SpeedChanged;
    public event Action? FullscreenClicked;
    public event Action? PlaylistClicked;

    public VideoControlBar()
    {
        _btnPrevious = AddButton("\u23EE");
        _btnPrevious.Click += (s, e) => PreviousClicked?.Invoke();

        _btnPlayPause = AddButton("\u25B6");
        _btnPlayPause.Click += (s, e) => PlayPauseClicked?.Invoke();

        _btnStop = AddButton("\u25A0");
        _btnStop.Click += (s, e) => StopClicked?.Invoke();

        _btnNext = AddButton("\u23ED");
        _btnNext.Click += (s, e) => NextClicked?.Invoke();

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

        _cmbSpeed = AddSpeedSelector();
        _cmbSpeed.SelectedIndexChanged += (s, e) =>
        {
            if (!_isUpdatingUI && _cmbSpeed.SelectedIndex >= 0)
                SpeedChanged?.Invoke(_cmbSpeed.SelectedIndex switch
                {
                    0 => 0.25,
                    1 => 0.5,
                    2 => 0.75,
                    4 => 1.25,
                    5 => 1.5,
                    6 => 2.0,
                    _ => 1.0
                });
        };

        AddGap(12);

        _btnFullscreen = AddButton("\u26F6");
        _btnFullscreen.Click += (s, e) => FullscreenClicked?.Invoke();

        _btnPlaylist = AddButton("\u2261");
        _btnPlaylist.Click += (s, e) => PlaylistClicked?.Invoke();

        _lblInfo = AddInfoLabel(160, ContentAlignment.MiddleLeft);
        StretchInfoLabel(_lblInfo);
    }

    public void UpdateUI()
    {
        _isUpdatingUI = true;
        bool slow = (++_tick & 3) == 0;

        try
        {
            _btnPlayPause.Text = Player.Paused ? "\u25B6" : "\u23F8";

            if (slow)
            {
                int playlistCount = Player.GetPropertyInt("playlist-count");
                _btnPrevious.Enabled = playlistCount > 1 && Player.PlaylistPos > 0;
                _btnNext.Enabled = playlistCount > 1 && Player.PlaylistPos < playlistCount - 1;

                _cmbSpeed.SelectedIndex = Player.GetPropertyDouble("speed") switch
                {
                    0.25 => 0,
                    0.5 => 1,
                    0.75 => 2,
                    1.25 => 4,
                    1.5 => 5,
                    2.0 => 6,
                    _ => 3
                };

                _lblInfo.Text = string.IsNullOrEmpty(Player.Path) ? "" : Path.GetFileName(Player.Path);
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
        finally
        {
            _isUpdatingUI = false;
        }
    }
}
