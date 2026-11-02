using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Font;
using Ofd.Layout.Element;
using Ofd.Layout.Element.Canvas;
using Ofd.Layout.Engine;
using Ofd.Layout.Engine.Render;
using Ofd.Pkg.Container;
using SkiaSharp;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>渲染处理器：段落文字、图片、Canvas 画板。</summary>
public class RendererTests : IDisposable
{
    private readonly string _root;
    private readonly ResManager _res;

    public RendererTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_render_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);

        var ofdDir = new OFDDir(Path.Combine(_root, "ofd"));
        var docDir = ofdDir.NewDoc();
        docDir.SetDocument(new Document().SetCommonData(new CT_CommonData().SetMaxUnitID(100)));
        _res = new ResManager(ofdDir, docDir, new AtomicCounter(100));
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

    private static List<TextObject> Texts(CT_PageBlock layer) =>
        layer.GetPageBlocks().OfType<TextObject>().ToList();

    [Fact]
    public void Paragraph_render_outputs_text_objects_with_alignment()
    {
        var layer = new CT_PageBlock();
        var p = new Paragraph(50, null).Add("abc").SetFontSize(3);
        p.SetX(10).SetY(20).SetMargin(1).SetBorder(2).SetPadding(3);

        p.DoPrepare(50);
        ParagraphRender.Render(layer, _res, p, new AtomicCounter(100));

        var txt = Assert.Single(Texts(layer));
        Assert.NotNull(txt.GetFont());
        Assert.Equal(3d, txt.GetSize(), 6);

        // x = 10+1+2+3 = 16；y = 20+1+2+3 = 24
        var boundary = txt.GetBoundary()!;
        Assert.Equal(16d, boundary.TopLeftX, 6);
        Assert.Equal(26d, boundary.TopLeftY, 6); // 20 + margin1 + border2 + padding3

        var code = Assert.Single(txt.GetTextCodes());
        Assert.Equal("abc", code.GetContent());
        // 定位点 Y = 行内最高文字高度
        Assert.True(code.GetY()!.Value > 0);
        Assert.NotNull(code.GetDeltaX());
        Assert.Equal(2, code.GetDeltaX()!.Size());
    }

    [Fact]
    public void Paragraph_render_skips_placeholder_spans()
    {
        var layer = new CT_PageBlock();
        var p = new Paragraph(50, null).SetFirstLineIndent(2).Add("abc").SetFontSize(3);

        p.DoPrepare(50);
        ParagraphRender.Render(layer, _res, p, new AtomicCounter(100));

        // 占位符不产出图元 → 只有 1 个文字对象
        Assert.Single(Texts(layer));
    }

    [Fact]
    public void Paragraph_render_draws_underline_and_opacity()
    {
        var layer = new CT_PageBlock();
        var p = new Paragraph(50, null);
        p.SetOpacity(0.5); // Div 的链式 setter 返回基类，此处分步调用
        p.Add(new Span("abc").SetFontSize(3).SetUnderline(true).SetColor(255, 0, 0));

        p.DoPrepare(50);
        ParagraphRender.Render(layer, _res, p, new AtomicCounter(100));

        var blocks = layer.GetPageBlocks();
        Assert.Equal(2, blocks.Count); // 文字 + 下划线

        var txt = Assert.IsType<TextObject>(blocks[0]);
        Assert.Equal(127, txt.GetAlpha());

        var underline = Assert.IsType<PathObject>(blocks[1]);
        Assert.True(underline.GetStroke());
        Assert.Equal(127, underline.GetAlpha());
    }

    [Fact]
    public void Paragraph_render_returns_nothing_for_empty_paragraph()
    {
        var layer = new CT_PageBlock();
        ParagraphRender.Render(layer, _res, null, new AtomicCounter(100));
        Assert.Empty(layer.GetPageBlocks());

        var empty = new Paragraph(50, 20);
        ParagraphRender.Render(layer, _res, empty, new AtomicCounter(100));
        Assert.Empty(layer.GetPageBlocks());
    }

    [Fact]
    public void Paragraph_render_stops_when_container_height_exceeded()
    {
        var layer = new CT_PageBlock();
        // 高度仅够 1 行（行高 = 3 + 2 = 5），但内容有 2 行
        // 使用带宽度映射表的 Times 字体，保证字符宽度可预期（约 0.5 倍字号）
        var p = new Paragraph(6, 5).SetLineSpace(2);
        p.Add(new Span("abcdefgh").SetFontSize(3).SetFont(Ofd.Font.FontName.TimesNewRoman.Font()));

        p.DoPrepare(6);
        Assert.True(p.GetLines().Count > 1, "应产生多行");

        ParagraphRender.Render(layer, _res, p, new AtomicCounter(100));

        Assert.True(Texts(layer).Count >= 1);
        Assert.True(Texts(layer).Count < p.GetLines().Count, "应舍弃超出行");
    }

    [Fact]
    public void Img_render_outputs_image_object()
    {
        string png = Path.Combine(_root, "img.png");
        using (var bmp = new SKBitmap(20, 10))
        using (var img = SKImage.FromBitmap(bmp))
        using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
        using (var fs = File.Create(png))
            data.SaveTo(fs);

        var layer = new CT_PageBlock();
        var image = new Img(30, 20, png);
        image.SetX(5).SetY(6).SetMargin(1).SetBorder(2).SetPadding(3).SetOpacity(0.5);

        ImgRender.Render(layer, _res, image, new AtomicCounter(100));

        var imgObj = Assert.IsType<ImageObject>(Assert.Single(layer.GetPageBlocks()));
        Assert.NotNull(imgObj.GetProxy().Attr("ResourceID"));

        // x = 5+1+2+3 = 11；y = 6+1+2+3 = 12
        var boundary = imgObj.GetBoundary()!;
        Assert.Equal(11d, boundary.TopLeftX, 6);
        Assert.Equal(12d, boundary.TopLeftY, 6);
        Assert.Equal(30d, boundary.Width, 6);
        Assert.Equal("30.0 0.0 0.0 20.0 0.0 0.0", imgObj.GetCTM()!.ToString());
        Assert.Equal(127, imgObj.GetAlpha());
    }

    [Fact]
    public void Img_render_rejects_invalid_path()
    {
        var layer = new CT_PageBlock();
        // ctor 会校验文件存在；先建对象再删除文件以触发渲染期校验
        string png = Path.Combine(_root, "gone.png");
        using (var bmp = new SKBitmap(2, 2))
        using (var img = SKImage.FromBitmap(bmp))
        using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
        using (var fs = File.Create(png))
            data.SaveTo(fs);

        var image = new Img(10, 10, png);
        File.Delete(png);
        Assert.Throws<ArgumentException>(() => ImgRender.Render(layer, _res, image, new AtomicCounter(100)));
    }

    [Fact]
    public void Canvas_render_creates_prefer_block_and_draws()
    {
        var layer = new CT_PageBlock();
        var canvas = new Canvas(40, 20, new Line(40, 20).SetBeginPoint(1, 1).SetEndPoint(10, 10));
        canvas.SetX(2).SetY(3).SetMargin(1);

        CanvasRender.Render(layer, _res, canvas, new AtomicCounter(100));

        // 新页块加入图层，并回写 preferBlock
        var block = Assert.IsType<CT_PageBlock>(Assert.Single(layer.GetPageBlocks()));
        // GetPageBlocks 返回新建的包装对象，比较底层 Dom 节点同一性
        Assert.Same(block.GetProxy(), canvas.GetPreferBlock()!.GetProxy());

        // 页块内含一条线段路径
        var path = Assert.IsType<PathObject>(Assert.Single(block.GetPageBlocks()));
        Assert.True(path.GetStroke());
        Assert.Equal("M 1 1 L 10 10", path.GetAbbreviatedDataText());

        // 再次渲染复用同一页块
        layer.GetProxy().Kids.Clear();
        CanvasRender.Render(layer, _res, canvas, new AtomicCounter(100));
        Assert.Empty(layer.GetPageBlocks()); // 未新建页块
        // GetPageBlocks 返回新建的包装对象，比较底层 Dom 节点同一性
        Assert.Same(block.GetProxy(), canvas.GetPreferBlock()!.GetProxy());
    }

    [Fact]
    public void Canvas_render_without_drawer_does_nothing()
    {
        var layer = new CT_PageBlock();
        var canvas = new Canvas(10, 10);
        CanvasRender.Render(layer, _res, canvas, new AtomicCounter(100));
        Assert.Empty(layer.GetPageBlocks());
    }
}
