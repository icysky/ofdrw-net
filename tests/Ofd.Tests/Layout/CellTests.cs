using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Layout.Element.Canvas;
using Ofd.Layout.Engine;
using Ofd.Pkg.Container;
using SkiaSharp;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>单元格：默认值、绘制器约束、文字换行与对齐、装饰线、图片。</summary>
public class CellTests : IDisposable
{
    private readonly string _root;

    public CellTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_cell_" + Guid.NewGuid().ToString("N")[..8]);
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
    public void Cell_defaults_match_java()
    {
        var cell = new Cell(40, 20);

        Assert.Null(cell.GetValue());
        Assert.Equal(CanvasTextAlign.Left, cell.GetTextAlign());
        Assert.Equal(VerticalAlign.Center, cell.GetVerticalAlign());
        Assert.Equal("#000000", cell.GetColor());
        Assert.Equal("宋体", cell.GetFontName());
        Assert.Equal(3d, cell.GetFontSize());
        Assert.Equal(0.6, cell.GetLineSpace());
        Assert.False(cell.GetBold());
        Assert.False(cell.GetItalic());
        Assert.Equal(0d, cell.GetLetterSpacing());
        Assert.Equal("normal", cell.GetFontWeight());
        Assert.False(cell.GetUnderline());
        Assert.False(cell.GetDeleteLine());
        Assert.Null(cell.GetImgPath());
        Assert.Equal(0d, cell.GetImgWidth());
        Assert.Equal("Canvas", cell.ElementType());

        // 绘制器为 CellContentDrawer
        Assert.IsType<CellContentDrawer>(cell.GetDrawer());
    }

    [Fact]
    public void Cell_rejects_other_drawers()
    {
        var cell = new Cell(10, 10);
        Assert.Throws<ArgumentException>(() => cell.SetDrawer(new Line(10, 10)));
        Assert.Throws<ArgumentException>(() => cell.SetDrawer(null));
    }

    [Fact]
    public void Cell_validation_rules()
    {
        var cell = new Cell(20, 10);

        Assert.Throws<ArgumentException>(() => cell.SetColor(null));
        Assert.Throws<ArgumentException>(() => cell.SetColor(""));
        Assert.Throws<ArgumentException>(() => cell.SetFontSize(0));
        Assert.Throws<ArgumentException>(() => cell.SetFont("", null));
        Assert.Throws<ArgumentException>(() => cell.SetFont("X", Path.Combine(_root, "nope.ttf")));
    }

    [Fact]
    public void Cell_draws_single_line_centered_vertically()
    {
        var cell = new Cell(40, 20).SetValue("abc");
        var (ctx, container) = NewContext();

        cell.GetDrawer().Draw(ctx);

        var txt = Assert.IsType<TextObject>(Assert.Single(container.GetPageBlocks()));
        var code = Assert.Single(txt.GetTextCodes());
        Assert.Equal("abc", code.GetContent());

        // 居中：(20 - 3.6)/2 + 3 = 11.2；左对齐 x = 0
        Assert.Equal(11.2, code.GetY()!.Value, 3);
        Assert.Equal(0d, code.GetX()!.Value, 3);
    }

    [Fact]
    public void Cell_vertical_align_variants()
    {
        var (ctx, container) = NewContext();

        var topCell = new Cell(40, 20).SetValue("abc").SetVerticalAlign(VerticalAlign.Top);
        topCell.GetDrawer().Draw(ctx);
        var top = Assert.IsType<TextObject>(container.GetPageBlocks()[0]);
        Assert.Equal(3d, top.GetTextCodes()[0].GetY()!.Value, 3); // 顶对齐 y = 字号

        container.GetProxy().Kids.Clear();
        var bottomCell = new Cell(40, 20).SetValue("abc").SetVerticalAlign(VerticalAlign.Bottom);
        bottomCell.GetDrawer().Draw(ctx);
        var bottom = Assert.IsType<TextObject>(container.GetPageBlocks()[0]);
        Assert.Equal(20 - 3.6 + 3, bottom.GetTextCodes()[0].GetY()!.Value, 3);
    }

    [Fact]
    public void Cell_wraps_long_text_into_multiple_lines()
    {
        // 单元格窄 → 自动换行产生多行
        var cell = new Cell(6, 20).SetValue("abcdefgh").SetFontSize(3);
        var (ctx, container) = NewContext();

        cell.GetDrawer().Draw(ctx);

        var texts = container.GetPageBlocks().OfType<TextObject>().ToList();
        Assert.True(texts.Count > 1, "应换行产生多行，实际: " + texts.Count);

        // 每行内容拼接应等于原文
        string joined = string.Concat(texts.Select(t => t.GetTextCodes()[0].GetContent()));
        Assert.Equal("abcdefgh", joined);
    }

    [Fact]
    public void Cell_honors_explicit_newline()
    {
        var cell = new Cell(40, 20).SetValue("ab\ncd");
        var (ctx, container) = NewContext();

        cell.GetDrawer().Draw(ctx);

        var texts = container.GetPageBlocks().OfType<TextObject>().ToList();
        Assert.Equal(2, texts.Count);
        Assert.Equal("ab", texts[0].GetTextCodes()[0].GetContent());
        Assert.Equal("cd", texts[1].GetTextCodes()[0].GetContent());

        // 第二行 y 增加 字号+行间距
        double y1 = texts[0].GetTextCodes()[0].GetY()!.Value;
        double y2 = texts[1].GetTextCodes()[0].GetY()!.Value;
        Assert.Equal(3.6, y2 - y1, 3);
    }

    [Fact]
    public void Cell_underline_and_delete_line_produce_paths()
    {
        var (ctx, container) = NewContext();

        var underlined = new Cell(40, 20).SetValue("abc").SetUnderline(true);
        underlined.GetDrawer().Draw(ctx);
        Assert.Equal(2, container.GetPageBlocks().Count); // 文字 + 下划线
        Assert.IsType<PathObject>(container.GetPageBlocks()[1]);

        container.GetProxy().Kids.Clear();
        var deleted = new Cell(40, 20).SetValue("abc").SetDeleteLine(true);
        deleted.GetDrawer().Draw(ctx);
        Assert.Equal(2, container.GetPageBlocks().Count);
        Assert.IsType<PathObject>(container.GetPageBlocks()[1]);
    }

    [Fact]
    public void Cell_draws_image_centered()
    {
        string png = Path.Combine(_root, "cell.png");
        using (var bmp = new SKBitmap(20, 10))
        using (var img = SKImage.FromBitmap(bmp))
        using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
        using (var fs = File.Create(png))
            data.SaveTo(fs);

        var (ctx, container) = NewContext();

        // 未指定宽高 → 按像素/PPM 换算
        var cell = new Cell(40, 20).SetImage(png);
        cell.GetDrawer().Draw(ctx);

        var imgObj = Assert.IsType<ImageObject>(Assert.Single(container.GetPageBlocks()));
        Assert.Equal(png, cell.GetImgPath());
        Assert.Equal(20 / 3.78, cell.GetImgWidth(), 6);
        Assert.Equal(10 / 3.78, cell.GetImgHeight(), 6);
        Assert.NotNull(imgObj.GetCTM());

        // 指定宽高 + 右对齐/底对齐
        container.GetProxy().Kids.Clear();
        var sized = new Cell(40, 20).SetImage(png, 10, 5)
            .SetTextAlign(CanvasTextAlign.Right)
            .SetVerticalAlign(VerticalAlign.Bottom);
        sized.GetDrawer().Draw(ctx);

        Assert.Equal(10d, sized.GetImgWidth(), 6);
        Assert.Equal(5d, sized.GetImgHeight(), 6);
        Assert.Single(container.GetPageBlocks());
    }

    [Fact]
    public void Cell_external_font_and_props_round_trip()
    {
        var cell = new Cell(40, 20)
            .SetValue("x")
            .SetColor("#FF0000")
            .SetFontName("黑体")
            .SetFontSize(4)
            .SetBold(true)
            .SetItalic(true)
            .SetLetterSpacing(0.5)
            .SetFontWeight("700")
            .SetLineSpace(1)
            .SetTextAlign(CanvasTextAlign.Center);

        Assert.Equal("#FF0000", cell.GetColor());
        Assert.Equal("黑体", cell.GetFontName());
        Assert.Equal(4d, cell.GetFontSize());
        Assert.True(cell.GetBold());
        Assert.True(cell.GetItalic());
        Assert.Equal(0.5, cell.GetLetterSpacing());
        Assert.Equal("700", cell.GetFontWeight());
        Assert.Equal(1d, cell.GetLineSpace());
        Assert.Equal(CanvasTextAlign.Center, cell.GetTextAlign());
    }
}
