using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Layout;
using Ofd.Layout.Element.Canvas;
using Ofd.Layout.Engine;
using Ofd.Pkg.Container;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>画板元素：Canvas 尺寸语义、CanvasBase 自绑定绘制器、Line 绘制输出。</summary>
public class CanvasElementTests : IDisposable
{
    private readonly string _root;

    public CanvasElementTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_canvas_" + Guid.NewGuid().ToString("N")[..8]);
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

    private (DrawContext Ctx, CT_PageBlock Container, ResManager Res) NewContext()
    {
        var ofdDir = new OFDDir(Path.Combine(_root, "ofd"));
        var docDir = ofdDir.NewDoc();
        docDir.SetDocument(new Document().SetCommonData(new CT_CommonData().SetMaxUnitID(100)));

        var res = new ResManager(ofdDir, docDir, new AtomicCounter(100));
        var container = new CT_PageBlock();
        var ctx = new DrawContext(container, new ST_Box(0, 0, 210, 297), new AtomicCounter(100), res);
        return (ctx, container, res);
    }

    [Fact]
    public void Canvas_keeps_size_and_binds_drawer()
    {
        int called = 0;
        var canvas = new Canvas(50, 20);
        Assert.Equal("Canvas", canvas.ElementType());
        Assert.Equal(50d, canvas.GetWidth());
        Assert.Null(canvas.GetDrawer());
        Assert.Null(canvas.GetPreferBlock());

        canvas.SetDrawer(new LambdaDrawer(_ => called++));
        canvas.SetMargin(1).SetPadding(2).SetBorder(0.5);

        // DoPrepare 忽略宽度限制
        var rect = canvas.DoPrepare(1);
        Assert.Equal(50 + canvas.WidthPlus(), rect.GetWidth(), 6);
        Assert.Equal(20 + canvas.HeightPlus(), rect.GetHeight(), 6);

        var (ctx, _, _) = NewContext();
        canvas.GetDrawer()!.Draw(ctx);
        Assert.Equal(1, called);

        // 优先页面块
        var block = new CT_PageBlock();
        canvas.SetPreferBlock(block);
        Assert.Same(block, canvas.GetPreferBlock());

        // 页面样式构造
        var fromStyle = new Canvas(PageLayout.A4());
        Assert.Equal(210d, fromStyle.GetWidth());
        Assert.Equal(297d, fromStyle.GetHeight());
    }

    [Fact]
    public void CanvasBase_sets_itself_as_drawer()
    {
        var line = new Line(30, 10);
        Assert.Same(line, line.GetDrawer());
        Assert.Equal("Canvas", line.ElementType());
    }

    [Fact]
    public void Line_draws_stroke_path_with_width_and_alpha()
    {
        var line = new Line(30, 10)
            .SetBeginPoint(1, 2)
            .SetEndPoint(5, 6)
            .SetLineWidth(2)
            .SetLineOpacity(0.5)
            .SetLineColor("red");

        var (ctx, container, res) = NewContext();
        line.Draw(ctx);

        var path = Assert.IsType<PathObject>(Assert.Single(container.GetPageBlocks()));
        Assert.True(path.GetStroke());
        // 线宽由绘制参数承载（图元自身不写 LineWidth）
        Assert.Null(path.GetLineWidth());
        var param = res.DocRes().GetResources().OfType<Ofd.Core.BasicStructure.Res.Resources.DrawParams>().First().GetDrawParams().Last();
        Assert.Equal(2d, param.GetLineWidth(), 6);
        Assert.Equal("M 1 2 L 5 6", path.GetAbbreviatedDataText());
        Assert.Equal(127, path.GetAlpha()); // 0.5 * 255

        // save/restore 生效：绘制后上下文线宽与透明度回到默认
        Assert.Equal(0.353, ctx.GetLineWidth(), 6);
        Assert.Equal("#000000", ctx.FillStyle);
    }

    [Fact]
    public void Line_skips_invalid_geometry()
    {
        var (ctx, container, res) = NewContext();

        // 线宽为 0
        new Line(10, 10).SetBeginPoint(0, 0).SetEndPoint(1, 1).SetLineWidth(0).Draw(ctx);
        Assert.Empty(container.GetPageBlocks());

        // 起终点相同
        new Line(10, 10).SetBeginPoint(3, 3).SetEndPoint(3, 3).Draw(ctx);
        Assert.Empty(container.GetPageBlocks());

        // 点数不足
        new Line(10, 10).SetBeginPoint(Array.Empty<double>()).SetEndPoint(1, 1).Draw(ctx);
        Assert.Empty(container.GetPageBlocks());

        // 颜色为 null → 兜底黑色仍可绘制
        var line = new Line(10, 10).SetBeginPoint(0, 0).SetEndPoint(10, 10).SetLineColor(null);
        line.Draw(ctx);
        Assert.Single(container.GetPageBlocks());
        Assert.Equal("#000000", line.GetLineColor());
    }

    [Fact]
    public void Line_accessors_round_trip()
    {
        var line = new Line(10, 10)
            .SetBeginPoint(new[] { 1d, 2d })
            .SetEndPoint(new[] { 3d, 4d })
            .SetLineColor("#00FF00")
            .SetLineWidth(1.5)
            .SetLineOpacity(0.8);

        Assert.Equal(new[] { 1d, 2d }, line.GetBeginPoint());
        Assert.Equal(new[] { 3d, 4d }, line.GetEndPoint());
        Assert.Equal("#00FF00", line.GetLineColor());
        Assert.Equal(1.5, line.GetLineWidth(), 6);
        Assert.Equal(0.8, line.GetLineOpacity(), 6);
    }

    private sealed class LambdaDrawer : IDrawer
    {
        private readonly Action<DrawContext> _action;

        public LambdaDrawer(Action<DrawContext> action) => _action = action;

        public void Draw(DrawContext ctx) => _action(ctx);
    }
}
