using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Core.Text.Text;
using Ofd.Font;
using Ofd.Layout.Element;
using Ofd.Layout.Element.Canvas;
using Ofd.Layout.Engine;
using Ofd.Pkg.Container;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>艺术字：属性与绘制效果（对齐、下划线、变换矩阵）。</summary>
public class ArtWordTests : IDisposable
{
    private readonly string _root;

    public ArtWordTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_art_" + Guid.NewGuid().ToString("N")[..8]);
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

    private (DrawContext Ctx, CT_PageBlock Container) NewContext()
    {
        var ofdDir = new OFDDir(Path.Combine(_root, "ofd"));
        var docDir = ofdDir.NewDoc();
        docDir.SetDocument(new Document().SetCommonData(new CT_CommonData().SetMaxUnitID(100)));

        var res = new ResManager(ofdDir, docDir, new AtomicCounter(100));
        var container = new CT_PageBlock();
        var ctx = new DrawContext(container, new ST_Box(0, 0, 210, 297), new AtomicCounter(100), res);
        return (ctx, container);
    }

    [Fact]
    public void ArtWord_defaults_match_java()
    {
        var art = new ArtWord(50, 20);

        Assert.Equal(3d, art.GetFontSize());
        Assert.Equal(0d, art.GetLetterSpacing());
        Assert.Equal(new[] { 0, 0, 0 }, art.GetColor());
        Assert.False(art.IsBold());
        Assert.False(art.IsItalic());
        Assert.False(art.IsUnderline());
        Assert.Equal(1.2d, art.GetUnderlineOffset(), 6);
        Assert.Equal(0d, art.GetUnderlineWidth(), 6);
        Assert.Equal(1d, art.GetHorizontalScaling(), 6);
        Assert.Equal(1d, art.GetVerticalScaling(), 6);
        Assert.Equal(0d, art.GetHorizontalInclination(), 6);
        Assert.Equal(0d, art.GetOffsetX(), 6);
        Assert.Equal("Canvas", art.ElementType());
        Assert.Same(art, art.GetDrawer()); // CanvasBase 自绑定
    }

    [Fact]
    public void ArtWord_color_parsing()
    {
        var art = new ArtWord(10, 10);

        art.SetColor(255, 128, 0);
        Assert.Equal(new[] { 255, 128, 0 }, art.GetColor());

        art.SetColor("#FF8000");
        Assert.Equal(new[] { 255, 128, 0 }, art.GetColor());

        art.SetColor(new[] { 1, 2, 3 });
        Assert.Equal(new[] { 1, 2, 3 }, art.GetColor());

        Assert.Throws<ArgumentException>(() => art.SetColor("#FFF"));
    }

    [Fact]
    public void ArtWord_draw_centered_text()
    {
        var art = new ArtWord(50, 20)
            .SetText("abc")
            .SetFontSize(5)
            .SetFont(Ofd.Font.FontName.TimesNewRoman.Font())
            .SetTextLayoutAlign(TextAlign.Center);

        var (ctx, container) = NewContext();
        // 预期宽度按艺术字实际字体（Times 5mm）测量
        double expectedWidth = TextMeasureTool.MeasureWidth("abc", new FontSetting(5, Ofd.Font.FontName.TimesNewRoman.Font())).Width;
        art.Draw(ctx);

        var txt = Assert.IsType<TextObject>(Assert.Single(container.GetPageBlocks()));
        var code = Assert.Single(txt.GetTextCodes());
        Assert.Equal("abc", code.GetContent());
        // 坐标写入 OFD 时按 3 位小数格式化，容差取 3 位
        Assert.Equal((50 - expectedWidth) / 2, code.GetX()!.Value, 3);
        Assert.Equal(5d, code.GetY()!.Value, 6);
    }

    [Fact]
    public void ArtWord_layout_align_variants()
    {
        var (ctx, container) = NewContext();
        double textWidth = TextMeasureTool.MeasureWidth("abc", new FontSetting(5, Ofd.Font.FontName.TimesNewRoman.Font())).Width;

        // 右对齐
        container.GetProxy().Kids.Clear();
        new ArtWord(50, 20).SetText("abc").SetFontSize(5)
            .SetFont(Ofd.Font.FontName.TimesNewRoman.Font())
            .SetTextLayoutAlign(TextAlign.Right)
            .Draw(ctx);
        var right = Assert.IsType<TextObject>(container.GetPageBlocks()[0]);
        Assert.Equal(50 - textWidth, right.GetTextCodes()[0].GetX()!.Value, 3);

        // 左对齐（默认）
        container.GetProxy().Kids.Clear();
        new ArtWord(50, 20).SetText("abc").SetFontSize(5)
            .SetFont(Ofd.Font.FontName.TimesNewRoman.Font())
            .Draw(ctx);
        var left = Assert.IsType<TextObject>(container.GetPageBlocks()[0]);
        Assert.Equal(0d, left.GetTextCodes()[0].GetX()!.Value, 6);
    }

    [Fact]
    public void ArtWord_draws_underline_path()
    {
        var art = new ArtWord(50, 20)
            .SetText("abc")
            .SetFontSize(10)
            .SetFont(Ofd.Font.FontName.TimesNewRoman.Font())
            .SetUnderline(true, 2, 0);

        var (ctx, container) = NewContext();
        art.Draw(ctx);

        Assert.Equal(2, container.GetPageBlocks().Count);
        var underline = Assert.IsType<PathObject>(container.GetPageBlocks()[1]);
        Assert.True(underline.GetStroke());
        Assert.EndsWith(" 12", underline.GetAbbreviatedDataText());
        Assert.StartsWith("M 0 12 L ", underline.GetAbbreviatedDataText());
    }

    [Fact]
    public void ArtWord_applies_transform_matrix()
    {
        var art = new ArtWord(50, 20)
            .SetText("ab")
            .SetFontSize(5)
            .SetFont(Ofd.Font.FontName.TimesNewRoman.Font());
        art.SetHorizontalScaling(0.5);
        art.SetVerticalScaling(2);
        art.SetHorizontalInclination(0.2);
        art.SetVerticalInclination(0.1);
        art.SetOffsetX(3);
        art.SetOffsetY(4);

        var (ctx, container) = NewContext();
        art.Draw(ctx);

        var txt = Assert.IsType<TextObject>(Assert.Single(container.GetPageBlocks()));
        Assert.Equal("0.5 0.1 0.2 2.0 3.0 4.0", txt.GetCTM()!.ToString());
    }

    [Fact]
    public void ArtWord_clone_keeps_art_effects()
    {
        var art = new ArtWord(50, 20)
            .SetText("abc")
            .SetFontSize(6)
            .SetColor(1, 2, 3)
            .SetWeight(Weight.W_700)
            .SetItalic(true)
            .SetUnderline(true, 1.5, 0.3);
        art.SetHorizontalScaling(0.8);
        art.SetVerticalScaling(1.2);
        art.SetOffsetX(2);
        art.SetTextLayoutAlign(TextAlign.Center);

        var clone = art.Clone();

        Assert.Equal("abc", clone.GetText());
        Assert.Equal(6d, clone.GetFontSize());
        Assert.Equal(new[] { 1, 2, 3 }, clone.GetColor());
        Assert.Equal(Weight.W_700, clone.GetWeight());
        Assert.True(clone.IsItalic());
        Assert.Equal(1.5, clone.GetUnderlineOffset(), 6);
        Assert.Equal(0.3, clone.GetUnderlineWidth(), 6);

        // 上游遗漏的艺术效果参数已补齐
        Assert.Equal(0.8, clone.GetHorizontalScaling(), 6);
        Assert.Equal(1.2, clone.GetVerticalScaling(), 6);
        Assert.Equal(2d, clone.GetOffsetX(), 6);
        Assert.Equal(TextAlign.Center, clone.GetTextLayoutAlign());

        // 颜色数组独立
        clone.SetColor(9, 9, 9);
        Assert.Equal(new[] { 1, 2, 3 }, art.GetColor());
    }
}
