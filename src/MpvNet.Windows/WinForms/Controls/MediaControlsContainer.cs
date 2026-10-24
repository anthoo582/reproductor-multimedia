using System.Drawing;
using System.Windows.Forms;
using MpvNet;

namespace MpvNet.Windows.WinForms.Controls;

// Las tres barras se crean una vez y se van mostrando según el tipo de medio.
// Solo una visible a la vez, así que el alto del contenedor nunca depende del contenido.
public class MediaControlsContainer : Panel
{
    readonly ImageControlBar _imageBar;
    readonly VideoControlBar _videoBar;
    readonly AudioControlBar _audioBar;

    MediaType _mediaType = MediaType.Unknown;

    public MediaControlsContainer()
    {
        Dock = DockStyle.Bottom;
        Height = MediaControlBar.LogicalHeight;
        BackColor = Color.FromArgb(32, 32, 32);

        _imageBar = new ImageControlBar { Visible = false };
        _videoBar = new VideoControlBar { Visible = false };
        _audioBar = new AudioControlBar { Visible = false };

        Controls.Add(_imageBar);
        Controls.Add(_videoBar);
        Controls.Add(_audioBar);
    }

    public void SetMediaType(MediaType mediaType)
    {
        _mediaType = mediaType;

        // sin medio conocido (idle, disco, url) no hay nada que controlar
        Visible = mediaType != MediaType.Unknown;

        _imageBar.Visible = mediaType == MediaType.Image;
        _videoBar.Visible = mediaType == MediaType.Video;
        _audioBar.Visible = mediaType == MediaType.Audio;

        if (Visible)
            UpdateUI();
    }

    public void UpdateUI()
    {
        if (!Visible || _mediaType == MediaType.Unknown)
            return;

        try
        {
            switch (_mediaType)
            {
                case MediaType.Image: _imageBar.UpdateUI(); break;
                case MediaType.Video: _videoBar.UpdateUI(); break;
                case MediaType.Audio: _audioBar.UpdateUI(); break;
            }
        }
        catch
        {
            // mpv puede no tener properties todavía; el siguiente tick reintenta
        }
    }
}
