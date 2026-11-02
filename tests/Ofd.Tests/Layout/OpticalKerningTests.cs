using System.IO.Compression;
using System.Text;
using Ofd.Layout;
using Ofd.Layout.Element;
using Ofd.Layout.Element.Canvas;
using Ofd.Reader;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>光学字距（ASCII 按字形墨迹 + 固定间隙推进）：默认关闭，开启后数字/字母宽度不再相同。</summary>
public class OpticalKerningTests : IDisposable
{
    private readonly string _root;

    public OpticalKerningTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_optical_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }
        catch
        {
            // ignore
        }
    }

    private sealed class TextDrawer : IDrawer
    {
        private readonly string _fontPath;
        private readonly bool _optical;

        public TextDrawer(string fontPath, bool optical)
        {
            _fontPath = fontPath;
            _optical = optical;
        }

        public void Draw(DrawContext ctx)
        {
            ctx.OpticalKerning = _optical;
            ctx.AddFont("测试字体", _fontPath);
            ctx.Font = "4mm 测试字体";
            ctx.FillText("0M1X", 10, 10);
        }
    }

    private double[] DrawAndReadDeltaX(string name, bool optical)
    {
        string path = Path.Combine(_root, name);
        using (var doc = new OFDDoc(path))
        {
            var fontPath = Ofd.Font.BuiltinFonts.FangSongPath
                           ?? throw new InvalidOperationException("缺少内置仿宋");

            doc.Add(new Canvas(60, 20, new TextDrawer(fontPath, optical)));
        }

        using var zip = ZipFile.OpenRead(path);
        var entry = zip.Entries.First(e => e.FullName.EndsWith("Content.xml", StringComparison.Ordinal));
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        string xml = reader.ReadToEnd();

        var match = System.Text.RegularExpressions.Regex.Match(xml, "DeltaX=\"([^\"]*)\"");
        Assert.True(match.Success, "未找到 DeltaX");

        return match.Groups[1].Value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(double.Parse)
            .ToArray();
    }

    [Fact]
    public void Default_kerning_uses_uniform_ascii_advance()
    {
        var widths = DrawAndReadDeltaX("default.ofd", optical: false);

        Assert.Equal(3, widths.Length);           // "0M1X" → 3 个位移
        Assert.All(widths, w => Assert.Equal(widths[0], w, 3));
    }

    [Fact]
    public void Optical_kerning_varies_ascii_advance_by_glyph_ink()
    {
        var widths = DrawAndReadDeltaX("optical.ofd", optical: true);

        Assert.Equal(3, widths.Length);
        Assert.True(widths.Distinct().Count() > 1, "光学字距未生效：所有 ASCII 位移仍相同");

        // 数字（0/1）墨迹窄于字母 M/X，位移应更小
        Assert.True(widths[0] < widths[1], $"0 的位移应小于 M：{widths[0]} vs {widths[1]}");
    }
}
