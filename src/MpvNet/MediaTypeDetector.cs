using System.Globalization;

namespace MpvNet;

// Fuente única de verdad de qué tipo de medio hay cargado. La capa de UI pregunta
// aquí y decide qué controles mostrar; nadie más clasifica por su cuenta.
public class MediaTypeDetector
{
    public event Action<MediaType>? MediaTypeChanged;

    MediaType _currentMediaType = MediaType.Unknown;

    public MediaType CurrentMediaType
    {
        get => _currentMediaType;
        private set
        {
            if (_currentMediaType == value)
                return;

            _currentMediaType = value;
            MediaTypeChanged?.Invoke(value);
        }
    }

    public MediaType DetectFromFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return MediaType.Unknown;

        string ext = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();

        if (FileTypes.IsImage(ext))
            return MediaType.Image;

        if (FileTypes.IsVideo(ext))
            return MediaType.Video;

        if (FileTypes.IsAudio(ext))
            return MediaType.Audio;

        return MediaType.Unknown;
    }

    // Se llama en file-loaded, cuando las track-list ya están pobladas. Un archivo
    // con extensión de vídeo pero sin pista visible (o solo cover-art) se trata como
    // audio: así se comportan también los .mp3 con carátula y los .mkv de solo audio.
    public MediaType DetectFromProperties()
    {
        string path = Player.GetPropertyString("path");

        if (string.IsNullOrEmpty(path))
            return MediaType.Unknown;

        MediaType byExtension = DetectFromFile(path);

        int trackCount = Player.GetPropertyInt("track-list/count");

        if (trackCount == 0)
            return byExtension;

        bool hasVideo = false;
        bool hasAudio = false;

        for (int i = 0; i < trackCount; i++)
        {
            string type = Player.GetPropertyString($"track-list/{i}/type");

            if (type == "video")
            {
                // image=yes marca carátulas embebidas, no un vídeo que mostrar
                if (Player.GetPropertyString($"track-list/{i}/image") != "yes")
                    hasVideo = true;
            }
            else if (type == "audio")
            {
                hasAudio = true;
            }
        }

        if (hasVideo)
            return MediaType.Video;

        if (hasAudio)
            return byExtension == MediaType.Image ? MediaType.Image : MediaType.Audio;

        return byExtension;
    }

    public void UpdateCurrentMediaType() => CurrentMediaType = DetectFromProperties();

    public void Reset() => CurrentMediaType = MediaType.Unknown;
}
