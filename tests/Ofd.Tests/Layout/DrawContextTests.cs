using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Core.PageDescription.DrawParam;
using Ofd.Layout.Element.Canvas;
using Ofd.Layout.Engine;
using Ofd.Pkg.Container;
using SkiaSharp;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>绘制上下文：路径/填充/描边、状态栈、变换与裁剪、文字、图片、颜色与线型。</summary>
public class DrawContextTests : IDisposable
{
    private readonly string _root;
    private readonly OFDDir _ofdDir;
    private readonly ResManager _res;
    private readonly AtomicCounter _counter;
    private readonly CT_PageBlock _container;
    private readonly ST_Box _boundary;

    public DrawContextTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_draw_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);

        _ofdDir = new OFDDir(Path.Combine(_root, "ofd"));
        var docDir = _ofdDir.NewDoc();
        docDir.SetDocument(new Document().SetCommonData(new CT_CommonData().SetMaxUnitID(100)));
        _res = new ResManager(_ofdDir, docDir, new AtomicCounter(100));
        _counter = new AtomicCounter(100);
        _container = new CT_PageBlock();
        _boundary = new ST_Box(0, 0, 210, 297);
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

    private DrawContext NewCtx() => new(_container, _boundary, _counter, _res);

    private static PathObject SinglePath(CT_PageBlock container) =>
        Assert.IsType<PathObject>(Assert.Single(container.GetPageBlocks()));

    [Fact]
    public void Fill_rect_creates_path_object_with_boundary_and_draw_param()
    {
        var ctx = NewCtx();
        ctx.FillRect(10, 10, 50, 20);

        var path = SinglePath(_container);
        Assert.True(path.GetFill());
        // Stroke 未显式设置（OFD 默认视作 true）
        Assert.Null(path.GetProxy().Attr("Stroke"));
        Assert.Equal("0 0 210 297", path.GetBoundary()!.ToString());
        Assert.NotNull(path.GetDrawParam());

        var data = path.GetAbbreviatedDataEle()!;
        var ops = data.ToList();
        Assert.Equal(5, ops.Count);
        Assert.Equal("M", ops[0].GetOpt());
        Assert.Equal(new double[] { 10, 10 }, ops[0].GetValues());
        Assert.Equal(new double[] { 60, 30 }, ops[2].GetValues());
        Assert.Equal("C", ops[4].GetOpt());
    }

    [Fact]
    public void Stroke_rect_and_fill_use_expected_flags()
    {
        var ctx = NewCtx();
        ctx.StrokeRect(1, 2, 3, 4);

        var strokePath = SinglePath(_container);
        Assert.True(strokePath.GetStroke());
        Assert.Null(strokePath.GetProxy().Attr("Fill"));

        // 路径式绘制
        ResetContainer();
        ctx.BeginPath().MoveTo(0, 0).LineTo(10, 0).Stroke();

        var path = SinglePath(_container);
        Assert.True(path.GetStroke());
        Assert.Equal("M 0 0 L 10 0", path.GetAbbreviatedDataText());

        // fill 会将线宽置 0
        ResetContainer();
        ctx.BeginPath().MoveTo(0, 0).LineTo(5, 5).Fill();
        var filled = SinglePath(_container);
        Assert.True(filled.GetFill());
        Assert.Equal(0d, filled.GetLineWidth()!.Value, 6);

        // 未开启路径时 stroke/fill 不产出对象
        ResetContainer();
        ctx.BeginPath();
        var empty = NewCtx();
        empty.Stroke();
        empty.Fill();
        Assert.Empty(_container.GetPageBlocks());
    }

    [Fact]
    public void Save_restore_keeps_style_state()
    {
        var ctx = NewCtx();
        ctx.SetFillColor(255, 0, 0);
        Assert.Equal(new[] { 255, 0, 0 }, ctx.GetFillColor());

        ctx.Save();
        ctx.SetFillColor(0, 0, 255);
        ctx.SetLineWidth(5);
        Assert.Equal(new[] { 0, 0, 255 }, ctx.GetFillColor());

        ctx.Restore();
        Assert.Equal(new[] { 255, 0, 0 }, ctx.GetFillColor());
        Assert.Equal(0.353, ctx.GetLineWidth(), 6); // 未被设置过，取默认

        // 空栈 restore 安全
        ctx.Restore();
    }

    [Fact]
    public void Colors_support_named_hex_and_invalid()
    {
        var ctx = NewCtx();

        ctx.SetFillColor(255, 128, 0);
        Assert.Equal("#FF8000", ctx.FillStyle);
        Assert.Equal(new[] { 255, 128, 0 }, ctx.GetFillColor());

        ctx.FillStyle = "red";
        Assert.Equal(new[] { 255, 0, 0 }, ctx.GetFillColor());

        // 非颜色对象 → null
        ctx.FillStyle = new CT_PageBlock();
        Assert.Null(ctx.GetFillColor());

        ctx.StrokeStyle = "not-a-color";
        Assert.Null(ctx.GetStrokeColor());
    }

    [Fact]
    public void Line_params_and_dash_validation()
    {
        var ctx = NewCtx();

        ctx.SetLineWidth(-1);
        Assert.Equal(0.353, ctx.GetLineWidth(), 6);
        ctx.SetLineWidth(2);
        Assert.Equal(2, ctx.GetLineWidth(), 6);

        ctx.SetLineCap(LineCapType.Round);
        Assert.Equal(LineCapType.Round, ctx.GetLineCap());
        ctx.SetLineJoin(LineJoinType.Bevel);
        Assert.Equal(LineJoinType.Bevel, ctx.GetLineJoin());
        ctx.SetMiterLimit(4);
        Assert.Equal(4, ctx.GetMiterLimit());

        ctx.SetLineDash(3, 2);
        Assert.Equal("3 2", ctx.GetDashPattern()!.ToString());
        Assert.Equal(0d, ctx.GetDashOffset()!.Value, 6); // 未设置时默认 0

        ctx.SetLineDash(1, new double[] { 5, 2 });
        Assert.Equal(1, ctx.GetDashOffset());
        Assert.Equal("5 2", ctx.GetDashPattern()!.ToString());

        // 清除
        ctx.SetLineDash(null, null);
        Assert.Null(ctx.GetDashPattern());
        Assert.Equal(0d, ctx.GetDashOffset()!.Value, 6);

        // 非法
        Assert.Throws<ArgumentException>(() => ctx.SetLineDash(null, 3));
    }

    [Fact]
    public void Global_alpha_is_clamped_and_applied()
    {
        var ctx = NewCtx();
        Assert.Null(ctx.GetGlobalAlpha());

        ctx.SetGlobalAlpha(0.5);
        ctx.FillRect(0, 0, 10, 10);
        Assert.Equal(127, SinglePath(_container).GetAlpha());

        ctx.SetGlobalAlpha(2);
        Assert.Equal(1, ctx.GetGlobalAlpha());
        ctx.SetGlobalAlpha(-1);
        Assert.Equal(0, ctx.GetGlobalAlpha());
        ctx.SetGlobalAlpha(null);
        Assert.Equal(1, ctx.GetGlobalAlpha());
    }

    [Fact]
    public void Transform_and_clip_affect_path_and_clips()
    {
        var ctx = NewCtx();

        // 变换矩阵：平移 (5,5) 后绘制路径，路径坐标被转换到裁剪区（clip 时才应用）
        ctx.Translate(5, 5);
        ctx.BeginPath().MoveTo(0, 0).LineTo(10, 0).Clip();
        ctx.FillRect(0, 0, 1, 1);

        var path = SinglePath(_container);
        var clips = path.GetClips();
        Assert.NotNull(clips);

        var clipPath = clips!.GetClips()[0].GetAreas()[0].GetClipObj() as Ofd.Core.Graph.PathObj.CT_Path;
        Assert.NotNull(clipPath);
        // 裁剪区受逆矩阵补偿
        Assert.NotNull(clipPath!.GetCTM());

        // setTransform 重置矩阵
        ctx.SetTransform(1, 0, 0, 1, 0, 0);
        ResetContainer();
        ctx.FillRect(0, 0, 1, 1);
        // 与 Java 一致：矩阵被显式重置为 identity 时仍会写入图元
        Assert.Equal("1.0 0.0 0.0 1.0 0.0 0.0", SinglePath(_container).GetCTM()!.ToString());
    }

    [Fact]
    public void Fill_text_produces_text_object_with_delta()
    {
        var ctx = NewCtx().SetDefaultFont(5);
        ctx.FillText("abc", 10, 20);

        var txt = Assert.IsType<TextObject>(Assert.Single(_container.GetPageBlocks()));
        Assert.NotNull(txt.GetFont());
        Assert.Equal(5d, txt.GetSize(), 6);
        Assert.Equal("0 0 210 297", txt.GetBoundary()!.ToString());
        Assert.True(txt.GetFill());

        var code = Assert.Single(txt.GetTextCodes());
        Assert.Equal("abc", code.GetContent());
        Assert.Equal(10d, code.GetX()!.Value, 6);
        Assert.Equal(20d, code.GetY()!.Value, 6);

        var deltaX = code.GetDeltaX();
        Assert.NotNull(deltaX);
        Assert.Equal(2, deltaX!.Size());

        // 空文本不产出
        ResetContainer();
        ctx.FillText("   ", 0, 0);
        Assert.Empty(_container.GetPageBlocks());

        // 竖排 → deltaY
        ctx.SetDefaultFont(5).GetFont().SetReadDirection(90);
        ctx.FillText("ab", 0, 0);
        var vertical = Assert.IsType<TextObject>(_container.GetPageBlocks()[0]);
        var vCode = Assert.Single(vertical.GetTextCodes());
        Assert.NotNull(vCode.GetDeltaY());
        Assert.Null(vCode.GetDeltaX());
    }

    [Fact]
    public void Measure_text_and_area()
    {
        var ctx = NewCtx().SetDefaultFont(10);

        var metrics = ctx.MeasureText("abc");
        Assert.Equal(0, metrics.ReadDirection);
        Assert.Equal(10d, metrics.FontSize);
        Assert.True(metrics.Width > 0);
        Assert.Equal(2, metrics.Offset!.Length);

        var area = ctx.MeasureTextArea("abc");
        Assert.Equal(3, area.CharAreas!.Count);
        Assert.True(area.Width > 0);
        Assert.True(area.Height > 0);
    }

    [Fact]
    public void Draw_image_registers_resource_and_applies_ctm()
    {
        string png = Path.Combine(_root, "img.png");
        using (var bmp = new SKBitmap(20, 10))
        using (var img = SKImage.FromBitmap(bmp))
        using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
        using (var fs = File.Create(png))
            data.SaveTo(fs);

        var ctx = NewCtx();
        ctx.DrawImage(png, 5, 6);

        var imgObj = Assert.IsType<ImageObject>(Assert.Single(_container.GetPageBlocks()));
        Assert.NotNull(imgObj.GetProxy().Attr("ResourceID"));
        Assert.NotNull(imgObj.GetCTM());
        Assert.Equal("0 0 210 297", imgObj.GetBoundary()!.ToString());

        // 尺寸按 PPM 换算：20px / 3.78
        Assert.Equal(20 / 3.78, ctx.Mm(20), 6);
        Assert.Equal(37, ctx.Pixel(10));
        Assert.Equal(2, _res.GetNewResIds().Count); // 图片资源 + 绘制参数资源

        // 指定宽高
        ResetContainer();
        ctx.DrawImage(png, 1, 2, 30, 40);
        Assert.NotNull(Assert.IsType<ImageObject>(SingleBlock(_container)).GetCTM());

        Assert.Throws<ArgumentException>(() => ctx.DrawImage(Path.Combine(_root, "nope.png"), 0, 0));
    }

    /// <summary>清空容器（GetPageBlocks 返回的是快照列表，需直接清空 DOM 子节点）。</summary>
    private void ResetContainer() => _container.GetProxy().Kids.Clear();

    private static IPageBlockType SingleBlock(CT_PageBlock container)
    {
        var blocks = container.GetPageBlocks();
        Assert.Single(blocks);
        return blocks[0];
    }

    [Fact]
    public void Gradient_and_pattern_factories()
    {
        var ctx = NewCtx();

        var linear = ctx.CreateLinearGradient(0, 0, 100, 0);
        linear.AddColorStop(0, "red");
        Assert.Single(linear.AxialShd.GetSegments());

        var radial = ctx.CreateRadialGradient(0, 0, 1, 0, 0, 10);
        radial.AddColorStop(0, "blue");
        Assert.Single(radial.RadialShd.GetSegments());

        // 渐变作为填充色 → CT_Color（CT_AxialShd 颜色族）
        ctx.FillStyle = linear;
        ctx.FillRect(0, 0, 10, 10);
        var path = SinglePath(_container);
        Assert.NotNull(path.GetDrawParam());

        // 底纹需要真实图片
        string png = Path.Combine(_root, "pat.png");
        using (var bmp = new SKBitmap(10, 10))
        using (var img = SKImage.FromBitmap(bmp))
        using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
        using (var fs = File.Create(png))
            data.SaveTo(fs);

        var pattern = ctx.CreatePattern(png, "row");
        Assert.Equal(Ofd.Core.PageDescription.Color.Pattern.ReflectMethod.Row, pattern.Pattern.GetReflectMethod());
        Assert.Throws<ArgumentException>(() => ctx.CreatePattern(Path.Combine(_root, "nope.png"), "repeat"));
    }
}
