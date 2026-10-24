using System;
using System.Drawing;
using System.Windows.Forms;
using MpvNet;
using MpvNet.Windows.WinForms.Controls;
using static MpvNet.Windows.Native.WinApi;

namespace MpvNet.Windows.WinForms;

public partial class MainForm
{
    PlaylistPanel? _playlistPanel;
    MediaTypeDetector? _mediaTypeDetector;
    MediaControlsContainer? _mediaControlsContainer;
    ContextualInputManager? _contextualInputManager;
    Panel? _mediaArea;
    Timer? _uiUpdateTimer;

    public MediaType MediaType => _mediaTypeDetector?.CurrentMediaType ?? MediaType.Unknown;

    // el contextual necesita saber dónde acaba el vídeo para anclar el zoom al cursor
    public Panel? MediaView => _mediaArea;

    // alto de la barra en píxeles reales, el designer no la escala porque se crea en runtime
    public int MediaToolbarHeight => _mediaControlsContainer?.Height ?? 0;

    void InitializeMediaSystem()
    {
        _mediaArea = MediaArea;

        // el handle del panel tiene que existir antes de que mpv cree su hijo dentro,
        // y las barras se cuelgan abajo así que se añaden después
        MediaArea.CreateControl();

        _mediaTypeDetector = new MediaTypeDetector();
        _mediaControlsContainer = new MediaControlsContainer();

        _mediaControlsContainer.Height = (int)(MediaControlBar.LogicalHeight * (DeviceDpi / 96f));

        Controls.Add(_mediaControlsContainer);
        MinimumSize = new Size(800, 600 + _mediaControlsContainer.Height);

        _contextualInputManager = new ContextualInputManager(this);
        _mediaTypeDetector.MediaTypeChanged += OnMediaTypeChanged;

        // la ventana de vídeo de mpv está deshabilitada a propósito, así que el panel
        // es quien recibe el ratón; el arrastre se queda aquí y no depende de mpv
        _mediaArea.MouseDown += MediaArea_MouseDown;
        _mediaArea.MouseMove += MediaArea_MouseMove;
        _mediaArea.MouseUp += MediaArea_MouseUp;
        _mediaArea.MouseCaptureChanged += MediaArea_MouseCaptureChanged;

        GuiCommand.Current.TogglePlaylistPanel += TogglePlaylistPanel;
        Player.PlaylistPosChanged += (pos) =>
        {
            if (_playlistPanel?.Visible == true)
                _playlistPanel.RefreshPlaylist();
        };

        ConnectControlEvents();

        FormClosed += (_, _) => ReleaseMediaSystem();

        // con 250 ms sobra para una barra de progreso y baja mucho el número de
        // consultas síncronas a mpv; en imágenes ni eso, solo cambia al hacer zoom
        _uiUpdateTimer = new Timer { Interval = 250 };
        _uiUpdateTimer.Tick += UIUpdateTimer_Tick;
        _uiUpdateTimer.Start();
    }

    void MediaArea_MouseDown(object? sender, MouseEventArgs e)
    {
        if (_mediaArea == null || _contextualInputManager == null || !_contextualInputManager.WantsDrag(e.Button))
            return;

        _mediaArea.Capture = true;
        _mediaArea.Cursor = Cursors.SizeAll;
        _contextualInputManager.HandleMouseDown(PointToScreen(e.Location), e.Button);
    }

    void MediaArea_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_mediaArea == null || _contextualInputManager?.IsPanning != true)
            return;

        _contextualInputManager.HandleMouseMove(PointToScreen(e.Location), Control.MouseButtons);
    }

    void MediaArea_MouseUp(object? sender, MouseEventArgs e)
    {
        if (_contextualInputManager?.IsPanning != true)
            return;

        _contextualInputManager.HandleMouseUp();

        if (_mediaArea != null)
        {
            _mediaArea.Cursor = Cursors.Default;
            _mediaArea.Capture = false;
        }
    }

    void MediaArea_MouseCaptureChanged(object? sender, EventArgs e)
    {
        // si otra ventana se lleva la captura nos quedamos sin BUTTONUP
        if (_mediaArea?.Capture == false)
        {
            _contextualInputManager?.HandleMouseUp();
            if (_mediaArea != null)
                _mediaArea.Cursor = Cursors.Default;
        }
    }

    void ReleaseMediaSystem()
    {
        _uiUpdateTimer?.Stop();
    }

    // Al abrir una imagen se centra el área donde mpv pinta, no la ventana entera:
    // la barra inferior descentraba la foto unos píxeles hacia arriba.
    void CenterWindowOnScreen()
    {
        if (_mediaArea == null || WindowState != FormWindowState.Normal || Player.Fullscreen)
            return;

        Rectangle area = Screen.FromControl(this).WorkingArea;
        Point mediaCenter = _mediaArea.PointToScreen(new Point(_mediaArea.Width / 2, _mediaArea.Height / 2));

        Left += area.X + area.Width / 2 - mediaCenter.X;
        Top += area.Y + area.Height / 2 - mediaCenter.Y;
    }

    void EnsurePlaylistPanel()
    {
        if (_playlistPanel != null)
            return;

        _playlistPanel = new PlaylistPanel { Visible = false };
        Controls.Add(_playlistPanel);
        _playlistPanel.BringToFront();
    }

    void TogglePlaylistPanel()
    {
        EnsurePlaylistPanel();

        if (_playlistPanel == null)
            return;

        _playlistPanel.Visible = !_playlistPanel.Visible;

        if (_playlistPanel.Visible)
        {
            _playlistPanel.RefreshPlaylist();
            _playlistPanel.BringToFront();
        }
    }

    void ConnectControlEvents()
    {
        var imageControls = GetImageControlBar();
        var videoControls = GetVideoControlBar();
        var audioControls = GetAudioControlBar();

        imageControls?.PreviousClicked += () => Player.Command("playlist-prev");
        imageControls?.NextClicked += () => Player.Command("playlist-next");
        imageControls?.ZoomInClicked += () => _contextualInputManager?.ZoomBy(0.1);
        imageControls?.ZoomOutClicked += () => _contextualInputManager?.ZoomBy(-0.1);
        imageControls?.ZoomFitClicked += () => _contextualInputManager?.ResetView();
        imageControls?.Zoom100Clicked += ZoomToActualSize;
        imageControls?.RotateLeftClicked += () => Player.Command("cycle-values video-rotate 270 180 90 0");
        imageControls?.RotateRightClicked += () => Player.Command("cycle-values video-rotate 90 180 270 0");
        imageControls?.FullscreenClicked += ToggleFullscreen;
        imageControls?.PlaylistClicked += TogglePlaylistPanel;

        videoControls?.PreviousClicked += () => Player.Command("playlist-prev");
        videoControls?.PlayPauseClicked += () => Player.Command("cycle pause");
        videoControls?.StopClicked += () => Player.Command("stop");
        videoControls?.NextClicked += () => Player.Command("playlist-next");
        videoControls?.TimelineChanged += percent =>
        {
            double duration = Player.GetPropertyDouble("duration");

            if (duration > 0)
                Player.SetPropertyDouble("time-pos", duration * percent);
        };
        videoControls?.VolumeChanged += volume => Player.SetPropertyInt("volume", volume);
        videoControls?.MuteClicked += () => Player.Command("cycle mute");
        videoControls?.SpeedChanged += speed => Player.SetPropertyDouble("speed", speed);
        videoControls?.FullscreenClicked += ToggleFullscreen;
        videoControls?.PlaylistClicked += TogglePlaylistPanel;

        audioControls?.PreviousClicked += () => Player.Command("playlist-prev");
        audioControls?.PlayPauseClicked += () => Player.Command("cycle pause");
        audioControls?.StopClicked += () => Player.Command("stop");
        audioControls?.NextClicked += () => Player.Command("playlist-next");
        audioControls?.TimelineChanged += percent =>
        {
            double duration = Player.GetPropertyDouble("duration");

            if (duration > 0)
                Player.SetPropertyDouble("time-pos", duration * percent);
        };
        audioControls?.VolumeChanged += volume => Player.SetPropertyInt("volume", volume);
        audioControls?.MuteClicked += () => Player.Command("cycle mute");
        audioControls?.ShuffleChanged += enabled => Player.SetPropertyBool("playlist-shuffle", enabled);
        audioControls?.RepeatChanged += mode =>
        {
            switch (mode)
            {
                case 0:
                    Player.SetPropertyString("loop-playlist", "no");
                    Player.SetPropertyString("loop-file", "no");
                    break;
                case 1:
                    Player.SetPropertyString("loop-playlist", "inf");
                    Player.SetPropertyString("loop-file", "no");
                    break;
                case 2:
                    Player.SetPropertyString("loop-file", "inf");
                    break;
            }
        };
        audioControls?.PlaylistClicked += TogglePlaylistPanel;
    }

    void ToggleFullscreen() => Player.Command("cycle fullscreen");

    // mpv ajusta con letterbox al tamaño de la ventana, así que para ver la imagen a
    // resolución real hay que compensar el escalado que él ya aplicó
    void ZoomToActualSize()
    {
        if (_mediaArea == null)
            return;

        double width = Player.GetPropertyInt("width");
        double height = Player.GetPropertyInt("height");

        if (width <= 0 || height <= 0 || _mediaArea.Width <= 0 || _mediaArea.Height <= 0)
            return;

        double fitScale = Math.Min(_mediaArea.Width / width, _mediaArea.Height / height);

        if (fitScale <= 0)
            return;

        _contextualInputManager?.SetZoom(Math.Log(1.0 / fitScale, 2.0));
    }

    void OnMediaTypeChanged(MediaType newType)
    {
        BeginInvoke(() =>
        {
            _mediaControlsContainer?.SetMediaType(newType);
            _contextualInputManager?.SetMediaType(newType);

            if (_uiUpdateTimer != null)
                _uiUpdateTimer.Interval = newType == MediaType.Image ? 1000 : 250;
        });
    }

    void UIUpdateTimer_Tick(object? sender, EventArgs e)
    {
        // al cambiar de salida mpv puede recrear su ventana; el handle se busca de
        // nuevo la próxima vez que haya que mandarle un mensaje
        if (MpvWindowHandle != IntPtr.Zero && !IsWindow(MpvWindowHandle))
            MpvWindowHandle = IntPtr.Zero;

        if (WindowState == FormWindowState.Minimized || !Visible)
            return;

        _mediaControlsContainer?.UpdateUI();
    }

    TBar? GetBar<TBar>() where TBar : Control
    {
        if (_mediaControlsContainer == null)
            return null;

        foreach (Control ctrl in _mediaControlsContainer.Controls)
            if (ctrl is TBar bar)
                return bar;

        return null;
    }

    ImageControlBar? GetImageControlBar() => GetBar<ImageControlBar>();
    VideoControlBar? GetVideoControlBar() => GetBar<VideoControlBar>();
    AudioControlBar? GetAudioControlBar() => GetBar<AudioControlBar>();
}
