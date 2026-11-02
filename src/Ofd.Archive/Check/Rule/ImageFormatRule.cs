using Ofd.Core.BasicStructure.Res;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 10：图像格式仅限 BMP/JPEG/PNG/JBIG2/JPEG2000/TIFF（GB/T 42133-2022 6.2.6e）。
/// <para>使用文件魔数检测实际格式，不信任 Format 属性。</para>
/// </summary>
public class ImageFormatRule : IArchiveRule
{
    public const string RULE_NAME = "IMAGE_FORMAT";

    private const string EXPECTED = "BMP/JPEG/PNG/JBIG2/JPEG2000/TIFF";

    /// <summary>允许的图像格式。</summary>
    private static readonly HashSet<string> AllowedFormats = new(StringComparer.Ordinal)
    {
        "BMP", "JPEG", "PNG", "JBIG2", "JPEG2000", "TIFF",
    };

    /// <summary>魔数表（顺序敏感：TIFF 用小端/大端两种）。</summary>
    private static readonly List<(string Format, byte[] Magic)> MagicBytes = new()
    {
        ("PNG", new byte[] { 0x89, 0x50, 0x4E, 0x47 }),
        ("JPEG", new byte[] { 0xFF, 0xD8, 0xFF }),
        ("BMP", new byte[] { 0x42, 0x4D }),
        ("JBIG2", new byte[] { 0x97, 0x4A, 0x42, 0x32 }),
        ("TIFF", new byte[] { 0x49, 0x49, 0x2A, 0x00 }), // little-endian
        ("TIFF", new byte[] { 0x4D, 0x4D, 0x00, 0x2A }), // big-endian
    };

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();

        var resMgt = reader.GetResMgt();
        if (resMgt == null) return violations;

        try
        {
            foreach (var media in resMgt.GetMultiMedias())
            {
                if (media.GetType() != MediaType.Image) continue;

                var mediaFile = media.GetMediaFile();
                if (mediaFile == null) continue;

                string path = mediaFile.ToString();
                if (path.StartsWith('/')) path = path[1..];
                string filePath = Path.Combine(reader.GetWorkDir(), path);

                if (!File.Exists(filePath))
                {
                    violations.Add(new ArchiveViolation(
                        RULE_NAME, ArchiveViolation.Severity.Error,
                        "图像文件不存在: " + path, mediaFile, "不存在", EXPECTED));
                    continue;
                }

                string? detected = DetectFormat(filePath);
                if (detected == null)
                {
                    violations.Add(new ArchiveViolation(
                        RULE_NAME, ArchiveViolation.Severity.Warn,
                        "无法识别图像格式 (魔数不匹配): " + path, mediaFile, "未知", EXPECTED));
                }
                else if (!AllowedFormats.Contains(detected))
                {
                    violations.Add(new ArchiveViolation(
                        RULE_NAME, ArchiveViolation.Severity.Error,
                        "图像格式不在允许范围内: " + detected, mediaFile, detected, EXPECTED));
                }
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "检查图像格式时异常: " + e.Message));
        }

        return violations;
    }

    /// <summary>按文件魔数检测图像格式，无法识别返回 null。</summary>
    private static string? DetectFormat(string filePath)
    {
        try
        {
            using var fs = File.OpenRead(filePath);
            var header = new byte[12];
            int read = fs.Read(header, 0, header.Length);
            if (read < 4) return null;

            foreach (var (format, magic) in MagicBytes)
            {
                if (read < magic.Length) continue;
                if (header.Take(magic.Length).SequenceEqual(magic)) return format;
            }
        }
        catch (IOException)
        {
            // 读取失败视为无法识别
        }

        return null;
    }
}
