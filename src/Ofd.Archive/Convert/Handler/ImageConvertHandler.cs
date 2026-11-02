using Ofd.Core.BasicStructure.Res;
using Ofd.Core.BasicType;
using Ofd.Pkg.Container;
using Ofd.Reader;
using SkiaSharp;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器 15：图像格式转换（GB/T 42133-2022 6.2.6e）。
/// <para>不在允许列表中的图像格式转为 PNG，并同步更新 MultiMedia 的 Format 与 MediaFile。</para>
/// </summary>
public class ImageConvertHandler : IArchiveHandler
{
    /// <summary>允许保留的图像格式。</summary>
    private static readonly HashSet<string> AllowedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "BMP", "JPEG", "PNG", "JBIG2", "JPEG2000", "TIFF",
    };

    /// <summary>魔数表。</summary>
    private static readonly List<(string Format, byte[] Magic)> Magic = new()
    {
        ("PNG", new byte[] { 0x89, 0x50, 0x4E, 0x47 }),
        ("JPEG", new byte[] { 0xFF, 0xD8, 0xFF }),
        ("BMP", new byte[] { 0x42, 0x4D }),
        ("JBIG2", new byte[] { 0x97, 0x4A, 0x42, 0x32 }),
        ("TIFF", new byte[] { 0x49, 0x49, 0x2A, 0x00 }),
    };

    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        var resMgt = reader.GetResMgt();
        if (resMgt == null) return;

        var mediaList = resMgt.GetMultiMedias();
        if (mediaList == null) return;

        string workDir = reader.GetWorkDir();

        foreach (var media in mediaList)
        {
            if (media.GetType() != MediaType.Image) continue;

            var mediaFile = media.GetMediaFile();
            if (mediaFile == null) continue;

            string path = mediaFile.ToString();
            if (path.StartsWith('/')) path = path[1..];

            string imgPath = Path.Combine(workDir, path);
            if (!File.Exists(imgPath)) continue;

            string? format = DetectFormat(imgPath);
            if (format == null || AllowedFormats.Contains(format)) continue;

            // 转为 PNG
            try
            {
                using var bitmap = SKBitmap.Decode(imgPath);
                if (bitmap == null) continue; // 无法解码，跳过

                string pngPath = Path.ChangeExtension(imgPath, ".png");
                using (var image = SKImage.FromBitmap(bitmap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (var fs = File.Create(pngPath))
                    data.SaveTo(fs);

                File.Delete(imgPath);

                // 更新引用
                var parent = mediaFile.Parent();
                string newLoc = (string.IsNullOrEmpty(parent) ? "" : parent + "/") + Path.GetFileName(pngPath);
                media.SetMediaFile(new ST_Loc(newLoc));
                media.SetFormat("PNG");
            }
            catch (Exception)
            {
                // 单个图像转换失败不影响整体
            }
        }
    }

    /// <summary>魔数检测格式。</summary>
    private static string? DetectFormat(string file)
    {
        try
        {
            using var fs = File.OpenRead(file);
            var header = new byte[12];
            int read = fs.Read(header, 0, header.Length);

            foreach (var (format, magic) in Magic)
            {
                if (read < magic.Length) continue;
                if (header.Take(magic.Length).SequenceEqual(magic)) return format;
            }
        }
        catch (IOException)
        {
            // 忽略
        }

        return null;
    }
}
