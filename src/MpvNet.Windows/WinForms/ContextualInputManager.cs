using System;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MpvNet;

namespace MpvNet.Windows.WinForms;

// La rueda sobre el vídeo es siempre zoom, sea imagen, vídeo o carátula; el volumen se
// maneja desde la cuña de la barra. Las teclas de visor (flechas, +, -, R) siguen siendo
// solo para imágenes porque en vídeo ya tienen otro uso.
public class ContextualInputManager
{
    readonly MainForm _mainForm;


    MediaType _currentMediaType = MediaType.Unknown;
    bool _isPanning;
    Point _lastPanPosition;

    // copia local de la vista; se relee de mpv al empezar cada ráfaga por si la cambió
    // un atajo de teclado o el menú
    double _zoom;
    double _panX;
    double _panY;
    double _videoWidth;
    double _videoHeight;

    long _lastViewChange;

    // lo que falta por mandarle a mpv; solo interesa la última vista pedida
    sealed record ViewState(double Zoom, double PanX, double PanY, string Scale);

    ViewState? _viewToSend;
    int _isSending;
    string _sentScale = "";

    public ContextualInputManager(MainForm mainForm)
    {
        _mainForm = mainForm;
    }

    public bool IsPanning => _isPanning;

    public void SetMediaType(MediaType mediaType)
    {
        _currentMediaType = mediaType;
        EndPan();
        RefreshScaleQuality();
    }

    public bool HandleMouseWheel(int delta, Point positionInMediaArea)
    {
        // Ctrl+rueda y compañía son bindings de mpv (seek), no se tocan
        if (!WantsWheel)
            return false;

        // delta viene en múltiplos de 120 por muesca, menos en ruedas de alta resolución
        ZoomBy(delta / 120.0 * 0.15, positionInMediaArea);
        return true;
    }

    // el panel del vídeo pregunta esto antes de quedarse con el clic; las teclas y la
    // rueda siguen llegando por MainForm
    public bool WantsWheel => _currentMediaType != MediaType.Unknown && Control.ModifierKeys == Keys.None;

    // en vídeo sin ampliar el arrastre sigue moviendo la ventana y el clic sigue siendo
    // de mpv; solo nos lo quedamos cuando hay algo que desplazar
    public bool WantsDrag(MouseButtons button)
    {
        if ((button & (MouseButtons.Left | MouseButtons.Middle)) == 0)
            return false;

        return _currentMediaType == MediaType.Image || (_currentMediaType != MediaType.Unknown && _zoom > 0);
    }

    public bool HandleMouseDown(Point screenPos, MouseButtons button)
    {
        if (!WantsDrag(button))
            return false;

        _isPanning = true;
        _lastPanPosition = screenPos;
        return true;
    }

    public bool HandleMouseUp()
    {
        if (!_isPanning)
            return false;

        EndPan();
        return true;
    }

    public bool HandleKeyDown(KeyEventArgs e)
    {
        if (_currentMediaType != MediaType.Image)
            return false;

        switch (e.KeyCode)
        {
            case Keys.Left:
                Player.Command("playlist-prev");
                return true;
            case Keys.Right:
                Player.Command("playlist-next");
                return true;
            case Keys.Add:
            case Keys.Oemplus:
                ZoomBy(0.1);
                return true;
            case Keys.Subtract:
            case Keys.OemMinus:
                ZoomBy(-0.1);
                return true;
            case Keys.D0:
            case Keys.NumPad0:
                ResetView();
                return true;
            case Keys.R:
                Player.Command("cycle-values video-rotate 90 180 270 0");
                return true;
        }

        return false;
    }

    public bool HandleMouseMove(Point screenPos, MouseButtons buttons)
    {
        if (!_isPanning || (buttons & (MouseButtons.Left | MouseButtons.Middle)) == 0)
        {
            EndPan();
            return false;
        }

        SyncViewIfStale();

        double shown = FitScale() * Math.Pow(2, _zoom);

        if (shown > 0)
        {
            // el pan va en fracciones del vídeo ya ampliado: 1 px de ratón vale 1/tamaño
            _panX += (screenPos.X - _lastPanPosition.X) / (_videoWidth * shown);
            _panY += (screenPos.Y - _lastPanPosition.Y) / (_videoHeight * shown);
            SendView();
        }

        _lastPanPosition = screenPos;
        return true;
    }

    public void EndPan()
    {
        if (_isPanning)
            Cursor.Current = Cursors.Default;

        _isPanning = false;
    }

    public void ResetView() => SetZoom(0);

    // Mantengo el zoom dentro de un rango utilizable. Sin tope, un par de ruedas de más
    // dejaban video-zoom por las nubes y el renderizador de mpv se caía al pedir semejante
    // ampliación. 4 equivale a 1600%, más que suficiente para leer un detalle.
    const double MinZoom = -2.0;
    const double MaxZoom = 4.0;

    // fija un zoom concreto y centra (lo usan Fit, 1:1 y cada archivo nuevo)
    public void SetZoom(double zoom)
    {
        ReadViewFromMpv();

        _zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        _panX = 0;
        _panY = 0;
        SendView();
    }

    // video-zoom es exponencial: 0 deja la imagen ajustada, 1 la dobla, -1 la reduce a la mitad
    public void ZoomBy(double step, Point? anchor = null)
    {
        SyncViewIfStale();

        double fitScale = FitScale();
        Panel? area = _mainForm.MediaView;

        // audio sin carátula o archivo todavía sin abrir: no hay nada que ampliar
        if (fitScale <= 0 || area == null)
            return;

        double newZoom = Math.Clamp(_zoom + step, MinZoom, MaxZoom);

        if (anchor.HasValue)
        {
            // el punto del vídeo que estaba bajo el cursor tiene que seguir estándolo.
            // video-pan-x/y van en fracciones del vídeo ya ampliado, y el tamaño en
            // pantalla es ancho*fitScale*2^zoom, de ahí sale esta identidad
            double factor = Math.Pow(2, -newZoom) - Math.Pow(2, -_zoom);

            _panX += (anchor.Value.X - area.Width / 2.0) / (_videoWidth * fitScale) * factor;
            _panY += (anchor.Value.Y - area.Height / 2.0) / (_videoHeight * fitScale) * factor;
        }

        _zoom = newZoom;
        SendView();
    }

    // Durante una ráfaga de rueda o un arrastre la vista se lleva en local y no se le
    // pregunta nada a mpv (cada pregunta espera al core). Si llevamos un rato quietos
    // se relee, por si la cambió un atajo de teclado o el menú.
    void SyncViewIfStale()
    {
        if (Environment.TickCount64 - _lastViewChange > 400)
            ReadViewFromMpv();
    }

    void ReadViewFromMpv()
    {
        _zoom = Player.GetPropertyDouble("video-zoom");
        _panX = Player.GetPropertyDouble("video-pan-x");
        _panY = Player.GetPropertyDouble("video-pan-y");

        if (double.IsNaN(_zoom) || double.IsInfinity(_zoom))
            _zoom = 0;

        // un pan no finito (NaN/Infinity) hacía abortar a mpv en el render
        if (double.IsNaN(_panX) || double.IsInfinity(_panX))
            _panX = 0;

        if (double.IsNaN(_panY) || double.IsInfinity(_panY))
            _panY = 0;

        // dwidth/dheight ya traen el aspecto corregido (anamórficos), pero no el giro:
        // una foto vertical por EXIF o un vídeo de móvil llegan tumbados
        _videoWidth = Player.GetPropertyInt("dwidth");
        _videoHeight = Player.GetPropertyInt("dheight");

        int rotate = Player.GetPropertyInt("video-params/rotate");

        if (rotate == 90 || rotate == 270)
            (_videoWidth, _videoHeight) = (_videoHeight, _videoWidth);
    }

    // escala con la que mpv encaja el vídeo en el panel antes de aplicar video-zoom
    double FitScale()
    {
        Panel? area = _mainForm.MediaView;

        if (area == null || area.Width <= 0 || area.Height <= 0 || _videoWidth <= 0 || _videoHeight <= 0)
            return 0;

        return Math.Min(area.Width / _videoWidth, area.Height / _videoHeight);
    }

    void SendView()
    {
        _lastViewChange = Environment.TickCount64;

        double pixelScale = FitScale() * Math.Pow(2, _zoom);
        Panel? area = _mainForm.MediaView;

        if (pixelScale > 0 && area != null)
        {
            double shownWidth = _videoWidth * pixelScale;
            double shownHeight = _videoHeight * pixelScale;

            // el borde de la imagen no pasa del borde del panel, y si cabe entera se
            // queda centrada: así nunca se pierde fuera del encuadre
            double maxPanX = Math.Max(0, (shownWidth - area.Width) / (2 * shownWidth));
            double maxPanY = Math.Max(0, (shownHeight - area.Height) / (2 * shownHeight));

            _panX = Math.Clamp(_panX, -maxPanX, maxPanX);
            _panY = Math.Clamp(_panY, -maxPanY, maxPanY);
        }

        // Nada de bilineal, que es lo que dejaba todo borroso al ampliar. En imágenes
        // muy ampliadas (más de 2 píxeles de pantalla por píxel real) vecino más cercano,
        // para ver el píxel nítido como en un visor de fotos; por debajo de eso nearest
        // deforma (píxeles de tamaños distintos) y queda mejor spline36, que es afilado
        // y barato. En vídeo siempre lanczos: nearest en movimiento parpadea.
        string scale = "lanczos";

        if (_currentMediaType == MediaType.Image)
            scale = pixelScale > 2 ? "nearest" : "spline36";

        // Mandarle la vista a mpv bloquea hasta que termina el cuadro que esté pintando,
        // así que va en un hilo aparte y la UI nunca espera. Si mientras tanto llegan
        // más ruedas solo se guarda la última vista: mpv pinta a su ritmo y no se
        // acumula cola, que es lo que hacía que el zoom fuera a tirones.
        Volatile.Write(ref _viewToSend, new ViewState(_zoom, _panX, _panY, scale));

        if (Interlocked.Exchange(ref _isSending, 1) == 0)
            Task.Run(SendPendingViews);
    }

    void SendPendingViews()
    {
        while (true)
        {
            ViewState? view = Interlocked.Exchange(ref _viewToSend, null);

            if (view == null)
            {
                Volatile.Write(ref _isSending, 0);

                // pudo entrar otra justo al soltar el turno
                if (Volatile.Read(ref _viewToSend) == null || Interlocked.Exchange(ref _isSending, 1) == 1)
                    return;

                continue;
            }

            // un solo comando para todo: por separado mpv llegaba a pintar un cuadro
            // con el zoom nuevo y el pan viejo y la imagen daba un salto
            string command = string.Create(CultureInfo.InvariantCulture,
                $"no-osd set video-zoom {view.Zoom:0.####}; no-osd set video-pan-x {view.PanX:0.######}; no-osd set video-pan-y {view.PanY:0.######}");

            // cambiar de filtro rehace los shaders, solo cuando de verdad cambia
            if (view.Scale != _sentScale && _currentMediaType != MediaType.Unknown)
            {
                command = $"no-osd set scale {view.Scale}; {command}";
                _sentScale = view.Scale;
            }

            Player.Command(command);
        }
    }

    public void RefreshScaleQuality()
    {
        ReadViewFromMpv();
        SendView();
    }
}
