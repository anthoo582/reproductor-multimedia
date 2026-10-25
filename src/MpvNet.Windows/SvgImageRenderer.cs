using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using Svg;

namespace MpvNet.Windows;

// libmpv no sabe leer SVG, así que se rasteriza a PNG y se reproduce ese PNG.
// El cache lleva la fecha del archivo en la clave: si el SVG cambia se vuelve a
// generar, y si no, se reutiliza mientras la app siga abierta.
static class SvgImageRenderer
{
    const int MaxSide = 4096;

    static string CacheFolder => Path.Combine(Player.ConfigFolder, "svg-cache");

    public static string? Render(string svgPath)
    {
        try
        {
            var origin = new FileInfo(svgPath);

            if (!origin.Exists)
                return null;

            Directory.CreateDirectory(CacheFolder);

            string key = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(
                $"{origin.FullName}|{origin.LastWriteTimeUtc.Ticks}|{origin.Length}")))[..10];

            // el nombre original se conserva para que la lista y la barra se lean
            string pngPath = Path.Combine(CacheFolder,
                $"{Path.GetFileNameWithoutExtension(svgPath)}-{key}.png");

            if (File.Exists(pngPath) && File.GetLastWriteTimeUtc(pngPath) >= origin.LastWriteTimeUtc)
                return pngPath;

            SvgDocument document = SvgDocument.Open(svgPath);
            using Bitmap rendered = document.Draw();

            using Bitmap target = Downscale(rendered);
            target.Save(pngPath, ImageFormat.Png);

            return pngPath;
        }
        catch (Exception ex)
        {
            Terminal.WriteError($"No se pudo renderizar el SVG {svgPath}: {ex.Message}");
            return null;
        }
    }

    // un SVG de 8000 px de ancho tarda y ocupa memoria de más; para verlo en la
    // ventana sobra con 4096 y mpv lo reduce igual al encajarlo
    static Bitmap Downscale(Bitmap source)
    {
        if (source.Width <= MaxSide && source.Height <= MaxSide)
            return new Bitmap(source);

        float factor = Math.Min(MaxSide / (float)source.Width, MaxSide / (float)source.Height);
        int width = Math.Max(1, (int)(source.Width * factor));
        int height = Math.Max(1, (int)(source.Height * factor));

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        using var graphics = Graphics.FromImage(bitmap);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(source, 0, 0, width, height);

        return bitmap;
    }
}
