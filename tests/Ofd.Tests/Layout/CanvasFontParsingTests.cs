using System.IO.Compression;
using System.Text;
using Ofd.Layout;
using Ofd.Layout.Element.Canvas;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>
/// 画布字体样式字符串解析：字体族可包含空格（如 "Noto Serif SC"），
/// 字号/字重/斜体必须被正确识别（曾因从右取 token 导致字号回退为 1）。
/// </summary>
public class CanvasFontParsingTests : IDisposable
{
    private readonly string _root;

    public CanvasFontParsingTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_canvasfont_" + Guid.NewGuid().ToString("N")[..8]);
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

    private sealed class Drawer : IDrawer
    {
        private readonly string _fontPath;
        private readonly string _fontSetting;

        public Drawer(string fontPath, string fontSetting)
        {
            _fontPath = fontPath;
            _fontSetting = fontSetting;
        }

        public void Draw(DrawContext ctx)
        {
            ctx.AddFont("My Space Font", _fontPath);
            ctx.Font = _fontSetting;
            ctx.FillText("测试", 10, 10);
        }
    }

    private string Render(string name, string fontSetting)
    {
        string fontPath = Ofd.Font.BuiltinFonts.FangSongPath
                          ?? throw new InvalidOperationException("缺少内置仿宋");

        string path = Path.Combine(_root, name);
        using (var doc = new OFDDoc(path))
            doc.Add(new Canvas(50, 20, new Drawer(fontPath, fontSetting)));

        using var zip = ZipFile.OpenRead(path);
        var entry = zip.Entries.First(e => e.FullName.EndsWith("Content.xml", StringComparison.Ordinal));
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    [Fact]
    public void Font_family_with_spaces_keeps_font_size()
    {
        string xml = Render("space.ofd", "4mm My Space Font");

        var size = System.Text.RegularExpressions.Regex.Match(xml, "Size=\"([^\"]*)\"").Groups[1].Value;
        Assert.Equal("4", size);
    }

    [Fact]
    public void Bold_and_size_tokens_are_parsed_in_any_order_before_family()
    {
        string xml = Render("bold.ofd", "bold 5.5mm My Space Font");

        var size = System.Text.RegularExpressions.Regex.Match(xml, "Size=\"([^\"]*)\"").Groups[1].Value;
        var weight = System.Text.RegularExpressions.Regex.Match(xml, "Weight=\"([^\"]*)\"").Groups[1].Value;

        Assert.Equal("5.500", size);
        Assert.Equal("700", weight);
    }
}
