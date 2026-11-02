using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Layout.Element;
using Ofd.Layout.Engine;
using Ofd.Layout.Engine.Render;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>盒式模型渲染：背景、等宽/不等宽边框、虚线、透明度。</summary>
public class DivRenderTests
{
    private static CT_PageBlock NewLayer() => new CT_PageBlock();

    private static List<PathObject> Paths(CT_PageBlock layer) =>
        layer.GetPageBlocks().OfType<PathObject>().ToList();

    [Fact]
    public void Div_without_background_or_border_is_skipped()
    {
        var layer = NewLayer();
        var div = new Div(50, 10).SetX(10).SetY(20);

        DivRender.Render(layer, div, new AtomicCounter(100));

        Assert.Empty(layer.GetPageBlocks());
    }

    [Fact]
    public void Div_renders_background_rect()
    {
        var layer = NewLayer();
        var div = new Div(50, 10)
            .SetX(10).SetY(20)
            .SetMargin(1).SetPadding(2).SetBorder(3)
            .SetBackgroundColor(255, 0, 0);

        DivRender.Render(layer, div, new AtomicCounter(100));

        var paths = Paths(layer);
        Assert.Equal(2, paths.Count); // 背景 + 边框（四边等宽）

        var bg = paths[0];
        Assert.True(bg.GetFill());
        Assert.False(bg.GetStroke());

        // x = 10 + marginLeft(1) + border(3) = 14；y = 20 + 1 + 3 = 24
        var boundary = bg.GetBoundary()!;
        Assert.Equal(14d, boundary.TopLeftX, 6);
        Assert.Equal(24d, boundary.TopLeftY, 6);
        // w = padding 2 + 50 + 2 = 54；h = 2 + 10 + 2 = 14
        Assert.Equal(54d, boundary.Width, 6);
        Assert.Equal(14d, boundary.Height, 6);

        // 背景路径为 0,0 起的矩形
        Assert.Equal("M 0 0 L 54 0 L 54 14 L 0 14 C", bg.GetAbbreviatedDataText());
    }

    [Fact]
    public void Div_renders_equal_width_border_as_single_rect()
    {
        var layer = NewLayer();
        var div = new Div(50, 10).SetX(10).SetY(20).SetMargin(1).SetPadding(2).SetBorder(3)
            .SetBorderColor(0, 0, 255);

        DivRender.Render(layer, div, new AtomicCounter(100));

        var border = Assert.Single(Paths(layer));
        Assert.True(border.GetStroke());
        Assert.Equal(3d, border.GetLineWidth()!.Value, 6);

        // w = 3 + 2 + 50 + 2 + 3 = 60；h = 3 + 2 + 10 + 2 + 3 = 20
        var boundary = border.GetBoundary()!;
        Assert.Equal(11d, boundary.TopLeftX, 6);
        Assert.Equal(21d, boundary.TopLeftY, 6);
        Assert.Equal(60d, boundary.Width, 6);
        Assert.Equal(20d, boundary.Height, 6);

        // 路径内缩半个线宽：M 1.5 1.5 L 58.5 1.5 ...
        // 路径数值按 3 位小数格式化（1.500），此处只校验关键坐标出现
        var borderText = border.GetAbbreviatedDataText();
        Assert.StartsWith("M 1.5", borderText);
        Assert.Contains("58.5", borderText);
    }

    [Fact]
    public void Div_renders_unequal_borders_per_side()
    {
        var layer = NewLayer();
        var div = new Div(50, 10).SetX(0).SetY(0)
            .SetBorder(1, 2, 3, 4); // 上右下左

        DivRender.Render(layer, div, new AtomicCounter(100));

        var paths = Paths(layer);
        Assert.Equal(4, paths.Count);

        // 顶边：宽 4+50+2 = 56，高 1
        Assert.Equal(56d, paths[0].GetBoundary()!.Width, 6);
        Assert.Equal(1d, paths[0].GetBoundary()!.Height, 6);

        // 底边：y = 1 + 10 + 0 = 11（无 padding/margin），宽 56，高 3
        Assert.Equal(11d, paths[1].GetBoundary()!.TopLeftY, 6);
        Assert.Equal(3d, paths[1].GetBoundary()!.Height, 6);

        // 左边：x=0, y=1, 宽 4, 高 = 1+10+3-1-3 = 10
        Assert.Equal(0d, paths[2].GetBoundary()!.TopLeftX, 6);
        Assert.Equal(1d, paths[2].GetBoundary()!.TopLeftY, 6);
        Assert.Equal(4d, paths[2].GetBoundary()!.Width, 6);
        Assert.Equal(10d, paths[2].GetBoundary()!.Height, 6);

        // 右边：x = 4 + 50 = 54
        Assert.Equal(54d, paths[3].GetBoundary()!.TopLeftX, 6);
        Assert.Equal(2d, paths[3].GetBoundary()!.Width, 6);
    }

    [Fact]
    public void Div_applies_opacity_as_alpha()
    {
        var layer = NewLayer();
        var div = new Div(20, 10).SetX(0).SetY(0).SetBorder(1).SetOpacity(0.5);

        DivRender.Render(layer, div, new AtomicCounter(100));

        Assert.Equal(127, Assert.Single(Paths(layer)).GetAlpha());
    }

    [Fact]
    public void Div_applies_dash_pattern_and_offset()
    {
        var layer = NewLayer();

        // {3,2} → 偶数参数补 0 偏移 → {0,3,2}：写 pattern 不写 offset
        var d1 = new Div(20, 10).SetX(0).SetY(0).SetBorder(1).SetBorderDash(3, 2);
        DivRender.Render(layer, d1, new AtomicCounter(100));
        var p1 = Assert.Single(Paths(layer));
        Assert.Equal("3 2", p1.GetDashPattern()!.ToString());
        Assert.Null(p1.GetProxy().Attr("DashOffset"));

        // {1,3,2} → 奇数参数首项为偏移 → offset=1，pattern="3 2"
        var layer2 = NewLayer();
        var d2 = new Div(20, 10).SetX(0).SetY(0).SetBorder(1).SetBorderDash(1, 3, 2);
        DivRender.Render(layer2, d2, new AtomicCounter(100));
        var p2 = Assert.Single(Paths(layer2));
        Assert.Equal("3 2", p2.GetDashPattern()!.ToString());
        Assert.Equal(1d, double.Parse(p2.GetProxy().Attr("DashOffset")!), 6);
    }

    [Fact]
    public void Div_without_height_throws()
    {
        var layer = NewLayer();
        var div = new Div().SetX(0).SetY(0).SetWidth(20).SetBorder(1);

        Assert.Throws<ArgumentException>(() => DivRender.Render(layer, div, new AtomicCounter(100)));
    }

    [Fact]
    public void Eq_compares_with_formatting_precision()
    {
        Assert.True(DivRender.Eq(1, 1, 1, 1));
        Assert.True(DivRender.Eq(1.0000001, 1.0000002)); // 格式化后相同
        Assert.False(DivRender.Eq(1, 2));
        Assert.True(DivRender.Eq());
    }
}
