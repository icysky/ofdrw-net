using Ofd.Archive.Check;
using Ofd.Archive.Check.Rule;
using Ofd.Layout;
using Ofd.Layout.Element;
using Ofd.Layout.Element.Canvas;
using Ofd.Reader;
using SkiaSharp;
using Xunit;

namespace Ofd.Tests.Archive;

/// <summary>图像格式（魔数）与资源位置规则。</summary>
public class ImageAndResourceRuleTests : IDisposable
{
    private readonly string _root;

    public ImageAndResourceRuleTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_img_" + Guid.NewGuid().ToString("N")[..8]);
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

    private string CreatePng(string name)
    {
        string p = Path.Combine(_root, name);
        using var bmp = new SKBitmap(10, 10);
        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        using var fs = File.Create(p);
        data.SaveTo(fs);
        return p;
    }

    private string BuildDocWithImage(string name, string imagePath)
    {
        string path = Path.Combine(_root, name);
        using var doc = new OFDDoc(path);
        var canvas = new Canvas(50, 20, new ImageDrawer(imagePath));
        canvas.SetX(10);
        canvas.SetY(10);
        doc.Add(canvas);
        return path;
    }

    [Fact]
    public void Image_format_rule_passes_for_png()
    {
        string png = CreatePng("ok.png");
        string ofd = BuildDocWithImage("img-ok.ofd", png);

        using var reader = new OFDReader(ofd);
        Assert.Empty(new ImageFormatRule().Check(reader, reader.GetOFDDir()));
    }

    [Fact]
    public void Image_format_rule_warns_on_unknown_magic()
    {
        // 伪装成 png 的文本文件：魔数不匹配
        string fake = Path.Combine(_root, "fake.png");
        File.WriteAllText(fake, "not an image");

        string ofd = BuildDocWithImage("img-fake.ofd", fake);

        using var reader = new OFDReader(ofd);
        var violations = new ImageFormatRule().Check(reader, reader.GetOFDDir());

        var v = Assert.Single(violations);
        Assert.Equal(ArchiveViolation.Severity.Warn, v.GetSeverity());
        Assert.Contains("无法识别图像格式", v.GetDescription());
        Assert.Equal("未知", v.GetActualValue());
    }

    [Fact]
    public void Resource_placement_rule_passes_for_layout_output()
    {
        string png = CreatePng("res.png");
        string ofd = BuildDocWithImage("placement.ofd", png);

        using var reader = new OFDReader(ofd);
        Assert.Empty(new ResourcePlacementRule().Check(reader, reader.GetOFDDir()));
    }

    [Fact]
    public void Checker_reports_image_and_font_rules_on_image_doc()
    {
        string png = CreatePng("full.png");
        string ofd = BuildDocWithImage("full.ofd", png);

        var violations = new OFDArchiveChecker().Check(ofd);

        Assert.Contains(violations, v => v.GetRuleName() == DocTypeRule.RULE_NAME);
        Assert.DoesNotContain(violations, v => v.GetRuleName() == ImageFormatRule.RULE_NAME);
        Assert.DoesNotContain(violations, v => v.GetRuleName() == ResourcePlacementRule.RULE_NAME);
    }

    /// <summary>通过 Canvas 绘制图片的绘制器。</summary>
    private sealed class ImageDrawer : IDrawer
    {
        private readonly string _path;

        public ImageDrawer(string path) => _path = path;

        public void Draw(DrawContext ctx) => ctx.DrawImage(_path, 0, 0, 10, 10);
    }
}
