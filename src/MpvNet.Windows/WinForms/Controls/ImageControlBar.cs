using System.Drawing;
using System.Windows.Forms;
using MpvNet;

namespace MpvNet.Windows.WinForms.Controls;

public class ImageControlBar : MediaControlBar
{
    readonly Button _btnPrevious;
    readonly Button _btnNext;
    readonly Button _btnZoomOut;
    readonly Button _btnZoomIn;
    readonly Button _btnFit;
    readonly Button _btnActualSize;
    readonly Button _btnRotateLeft;
    readonly Button _btnRotateRight;
    readonly Button _btnFullscreen;
    readonly Button _btnPlaylist;
    readonly Label _lblInfo;

    public event Action? PreviousClicked;
    public event Action? NextClicked;
    public event Action? ZoomInClicked;
    public event Action? ZoomOutClicked;
    public event Action? ZoomFitClicked;
    public event Action? Zoom100Clicked;
    public event Action? RotateLeftClicked;
    public event Action? RotateRightClicked;
    public event Action? FullscreenClicked;
    public event Action? PlaylistClicked;

    public ImageControlBar()
    {
        _btnPrevious = AddButton("\u25C0");
        _btnPrevious.Click += (s, e) => PreviousClicked?.Invoke();

        _btnNext = AddButton("\u25B6");
        _btnNext.Click += (s, e) => NextClicked?.Invoke();

        AddGap();

        _btnZoomOut = AddButton("\u2212");
        _btnZoomOut.Click += (s, e) => ZoomOutClicked?.Invoke();

        _btnZoomIn = AddButton("+");
        _btnZoomIn.Click += (s, e) => ZoomInClicked?.Invoke();

        _btnFit = AddButton("Fit", 48);
        _btnFit.Click += (s, e) => ZoomFitClicked?.Invoke();

        _btnActualSize = AddButton("1:1", 48);
        _btnActualSize.Click += (s, e) => Zoom100Clicked?.Invoke();

        AddGap();

        _btnRotateLeft = AddButton("\u21B6");
        _btnRotateLeft.Click += (s, e) => RotateLeftClicked?.Invoke();

        _btnRotateRight = AddButton("\u21B7");
        _btnRotateRight.Click += (s, e) => RotateRightClicked?.Invoke();

        _btnFullscreen = AddButton("\u26F6");
        _btnFullscreen.Click += (s, e) => FullscreenClicked?.Invoke();

        _btnPlaylist = AddButton("\u2261");
        _btnPlaylist.Click += (s, e) => PlaylistClicked?.Invoke();

        _lblInfo = AddInfoLabel(240, ContentAlignment.MiddleLeft);
        StretchInfoLabel(_lblInfo);
    }

    string _lastInfo = "";

    public void UpdateUI()
    {
        try
        {
            int playlistCount = Player.GetPropertyInt("playlist-count");
            int playlistPos = Player.PlaylistPos;

            _btnPrevious.Enabled = playlistCount > 1 && playlistPos > 0;
            _btnNext.Enabled = playlistCount > 1 && playlistPos < playlistCount - 1;

            string path = Player.Path;

            if (string.IsNullOrEmpty(path))
            {
                _lblInfo.Text = "";
                _lastInfo = "";
                return;
            }

            int width = Player.VideoSize.Width;
            int height = Player.VideoSize.Height;

            string resolution = width > 0 && height > 0 ? $"{width}x{height} \u00B7 " : "";
            long zoom = (long)Math.Round(Math.Pow(2, Player.GetPropertyDouble("video-zoom", false)) * 100);
            string position = playlistCount > 0 ? $"{playlistPos + 1}/{playlistCount} \u00B7 " : "";
            string info = $"{resolution}{zoom}% \u00B7 {position}{Path.GetFileName(path)}";

            // en imágenes este timer corre a 1 Hz; no hace falta tocar el texto si nada cambió
            if (info != _lastInfo)
            {
                _lblInfo.Text = info;
                _lastInfo = info;
            }
        }
        catch
        {
            _btnPrevious.Enabled = false;
            _btnNext.Enabled = false;
        }
    }
}
