using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Svg;

const int maxSide = 4096;
string svgPath = Path.Combine(AppContext.BaseDirectory, "smoke.svg");

File.WriteAllText(svgPath, """
<svg xmlns="http://www.w3.org/2000/svg" width="800" height="600" viewBox="0 0 800 600">
  <rect width="800" height="600" fill="#1b1b1b"/>
  <circle cx="400" cy="300" r="200" fill="#d04648"/>
  <text x="400" y="320" font-size="64" text-anchor="middle" fill="#ffffff">SVG</text>
</svg>
""");

SvgDocument document = SvgDocument.Open(svgPath);
using Bitmap rendered = document.Draw();
using Bitmap target = Downscale(rendered, maxSide);

string pngPath = Path.Combine(AppContext.BaseDirectory, "smoke.png");
target.Save(pngPath, ImageFormat.Png);

Console.WriteLine($"rendered={rendered.Width}x{rendered.Height} png={new FileInfo(pngPath).Length} bytes");
Console.WriteLine(File.Exists(pngPath) ? "OK" : "FAIL");

static Bitmap Downscale(Bitmap source, int maxSide)
{
    if (source.Width <= maxSide && source.Height <= maxSide)
        return new Bitmap(source);

    float factor = Math.Min(maxSide / (float)source.Width, maxSide / (float)source.Height);
    var bitmap = new Bitmap((int)(source.Width * factor), (int)(source.Height * factor), PixelFormat.Format32bppArgb);

    using var graphics = Graphics.FromImage(bitmap);
    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
    graphics.DrawImage(source, 0, 0, bitmap.Width, bitmap.Height);

    return bitmap;
}
