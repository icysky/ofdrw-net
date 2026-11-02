using Ofd.Layout;
using Ofd.Layout.Element;
using Xunit;
using LayerType = Ofd.Core.BasicStructure.PageObj.Layer.Type;

namespace Ofd.Tests.Layout;

/// <summary>页面样式与盒式模型（PageLayout / Div）。</summary>
public class DivAndPageLayoutTests
{
    [Fact]
    public void Page_layout_presets_and_content_area()
    {
        var a4 = PageLayout.A4();
        Assert.Equal(210, a4.GetWidth());
        Assert.Equal(297, a4.GetHeight());

        // 默认边距：上下 25.4，左右 31.7
        Assert.Equal(new[] { 25.4, 31.7, 25.4, 31.7 }, a4.GetMargin());
        Assert.Equal(31.7, a4.GetStartX(), 6);
        Assert.Equal(25.4, a4.GetStartY(), 6);
        Assert.Equal(146.6, a4.ContentWidth(), 6);
        Assert.Equal(246.2, a4.ContentHeight(), 6);

        var area = a4.GetWorkerArea();
        Assert.Equal(146.6, area.GetWidth(), 6);
        Assert.Equal(246.2, area.GetHeight(), 6);

        // A0/A10 尺寸
        Assert.Equal(841, PageLayout.A0().GetWidth());
        Assert.Equal(26, PageLayout.A10().GetWidth());
    }

    [Fact]
    public void Page_layout_margin_normalization_and_page_area()
    {
        var p = new PageLayout(100, 200).SetMargin(10);
        Assert.Equal(new[] { 10d, 10, 10, 10 }, p.GetMargin());

        p.SetMargin(1, 2);
        Assert.Equal(new[] { 1d, 2, 1, 2 }, p.GetMargin());

        p.SetMargin(1, 2, 3);
        Assert.Equal(new[] { 1d, 2, 3, 0 }, p.GetMargin());

        p.SetMarginTop(5).SetMarginRight(6).SetMarginBottom(7).SetMarginLeft(8);
        Assert.Equal(new[] { 5d, 6, 7, 8 }, p.GetMargin());
        Assert.Equal(100 - 8 - 6, p.ContentWidth(), 6);
        Assert.Equal(200 - 5 - 7, p.ContentHeight(), 6);

        var area = p.GetPageArea();
        Assert.Equal("0 0 100 200", area.GetPhysicalBox()!.ToString());
        Assert.Equal("0 0 100 200", area.GetApplicationBox()!.ToString());
    }

    [Fact]
    public void Page_layout_clone_and_equals()
    {
        var a = PageLayout.A4();
        var b = a.Clone();
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());

        b.SetMarginLeft(1);
        Assert.NotEqual(a, b);
        Assert.Equal(31.7, a.GetMarginLeft(), 6); // 原对象不受影响
    }

    [Fact]
    public void Div_defaults_and_box_model_math()
    {
        var div = new Div();
        Assert.Equal(Clear.Both, div.GetClear());
        Assert.Equal(AFloat.Left, div.GetFloat());
        Assert.Equal(Position.Static, div.GetPosition());
        Assert.Equal(LayerType.Body, div.GetLayer());
        Assert.False(div.IsPlaceholder());
        Assert.True(div.IsNoBorder());

        div.SetMargin(1, 2, 3, 4).SetPadding(5).SetBorder(1, 2, 1, 2);
        // 额外宽 = 左右 margin(2+4) + padding(5+5) + border(2+2) = 20
        Assert.Equal(20, div.WidthPlus(), 6);
        // 额外高 = 上下 margin(1+3) + padding(5+5) + border(1+1) = 16
        Assert.Equal(16, div.HeightPlus(), 6);

        Assert.False(div.IsNoBorder());
        div.SetBorder(0);
        Assert.True(div.IsNoBorder());
    }

    [Fact]
    public void Div_padding_border_dash_and_color_rules()
    {
        var div = new Div();
        div.SetPadding(2);
        Assert.Equal(new[] { 2d, 2, 2, 2 }, div.GetPadding());

        div.SetPadding(1, 2);
        Assert.Equal(new[] { 1d, 2, 1, 2 }, div.GetPadding());

        div.SetPadding(1, 2, 3);
        Assert.Equal(new[] { 1d, 2, 3, 0 }, div.GetPadding());

        // 虚线：单参 → {0,d,d}
        div.SetBorderDash(3);
        Assert.Equal(new[] { 0d, 3, 3 }, div.GetBorderDash());

        // 偶数 → 补 0 偏移
        div.SetBorderDash(3, 2);
        Assert.Equal(new[] { 0d, 3, 2 }, div.GetBorderDash());

        // 奇数 → 首参为偏移
        div.SetBorderDash(1, 3, 2);
        Assert.Equal(new[] { 1d, 3, 2 }, div.GetBorderDash());

        // 取消虚线
        div.SetBorderDash();
        Assert.Null(div.GetBorderDash());

        // 颜色：支持名称与 16 进制
        div.SetBackgroundColor("red");
        Assert.Equal(new[] { 255, 0, 0 }, div.GetBackgroundColor());
        div.SetBorderColor("#00FF00");
        Assert.Equal(new[] { 0, 255, 0 }, div.GetBorderColor());
        Assert.Throws<ArgumentException>(() => div.SetBackgroundColor("not-a-color"));
        Assert.Throws<ArgumentException>(() => div.SetBorderColor("not-a-color"));
    }

    [Fact]
    public void Div_opacity_clamped_and_display_switch()
    {
        var div = new Div();
        div.SetOpacity(1.5);
        Assert.Equal(1, div.GetOpacity());
        div.SetOpacity(-0.5);
        Assert.Equal(0, div.GetOpacity());
        div.SetOpacity(null);
        Assert.Null(div.GetOpacity());

        // 默认 Clear.Both → 块级
        Assert.True(div.IsBlockElement());
        Assert.Equal(Display.Block, div.GetDisplay());

        div.SetDisplay(Display.InlineBlock);
        Assert.Equal(Clear.None, div.GetClear());
        Assert.False(div.IsBlockElement());
        Assert.Equal(Display.InlineBlock, div.GetDisplay());

        div.SetDisplay(Display.Block);
        Assert.Equal(Clear.Both, div.GetClear());

        // 绝对定位不是块级元素
        var abs = new Div(1, 2, 30, 40);
        Assert.Equal(Position.Absolute, abs.GetPosition());
        Assert.False(abs.IsBlockElement());
    }

    [Fact]
    public void Div_prepare_and_box()
    {
        var div = new Div();
        Assert.True(div.DoPrepare(100).IsEmpty()); // 宽高未定

        div.SetWidth(50).SetHeight(20).SetMargin(1).SetPadding(2).SetBorder(0.5);
        var rect = div.DoPrepare(100);
        Assert.Equal(50 + div.WidthPlus(), rect.GetWidth(), 6);
        Assert.Equal(20 + div.HeightPlus(), rect.GetHeight(), 6);
        Assert.Equal(50, div.GetWidth());

        // 超过宽度限制 → 收缩
        div.SetWidth(200);
        var rect2 = div.DoPrepare(100);
        Assert.Equal(100 - div.WidthPlus(), div.GetWidth()!.Value, 6);
        Assert.Equal(rect2.GetWidth(), div.GetWidth()!.Value + div.WidthPlus(), 6);

        Assert.Equal(div.GetWidth()!.Value + div.WidthPlus(), div.Box().GetWidth(), 6);
        Assert.Throws<ArgumentNullException>(() => new Div(10, 10).DoPrepare(null));
    }

    [Fact]
    public void Div_split_returns_self_when_enough_space()
    {
        var div = new Div(50, 20);
        var parts = div.Split(100);
        Assert.Single(parts);
        Assert.Same(div, parts[0]);
    }

    [Fact]
    public void Div_split_consumes_margin_first()
    {
        // 高度 20 + margin 上下各 10 → 总高 40；切分高度 5 只够消耗部分 margin
        var div = new Div(50, 20).SetMargin(10);
        var parts = div.Split(5);

        Assert.Equal(2, parts.Length);
        Assert.True(parts[0].IsPlaceholder()); // 只剩 margin 的占位符
        Assert.Equal(5d, parts[0].GetMarginTop(), 6);
        Assert.Equal(0d, parts[0].GetHeight()!.Value, 6);
        Assert.Equal(5d, parts[1].GetMarginTop(), 6); // 残留 margin
    }

    [Fact]
    public void Div_split_adjusts_content_height()
    {
        // 无 margin/border/padding：高度 100，切分高度 60
        var div = new Div(50, 100);
        var parts = div.Split(60);

        Assert.Equal(2, parts.Length);
        Assert.Equal(60, parts[0].GetHeight()!.Value, 6);
        Assert.Equal(40, parts[1].GetHeight()!.Value, 6);
        Assert.Equal(0d, parts[1].GetMarginTop(), 6);
    }

    [Fact]
    public void Div_clone_is_deep_for_arrays()
    {
        var div = new Div(10, 20).SetMargin(1, 2, 3, 4).SetBackgroundColor(1, 2, 3);
        var copy = div.Clone();

        copy.SetMarginLeft(99).SetBackgroundColor(9, 9, 9);

        Assert.Equal(4, div.GetMarginLeft(), 6);
        Assert.Equal(new[] { 1, 2, 3 }, div.GetBackgroundColor());
        Assert.Equal(99, copy.GetMarginLeft(), 6);
    }

    [Fact]
    public void Div_placeholder_factory_sets_flags()
    {
        var p = Div.Placeholder(30, 40, AFloat.Right);
        Assert.True(p.IsPlaceholder());
        Assert.Equal(30, p.GetWidth());
        Assert.Equal(40, p.GetHeight());
        Assert.Equal(AFloat.Right, p.GetFloat());
        Assert.Equal(Clear.None, p.GetClear());
        Assert.True(p.IsIntegrity());

        var p2 = Div.Placeholder(new Rectangle(10, 20), AFloat.Left);
        Assert.Equal(10, p2.GetWidth());
        Assert.Equal(20, p2.GetHeight());
    }

    [Fact]
    public void Div_element_type_and_render_finish_hook()
    {
        var div = new Div();
        Assert.Equal("Div", div.ElementType());

        bool called = false;
        div.OnRenderFinish((loc, ids, res) => called = true);
        Assert.NotNull(div.GetOnRenderFinish());

        div.GetOnRenderFinish()!(new Ofd.Core.BasicType.ST_Loc("/Doc_0/Pages/Page_0/Content.xml"),
            new List<Ofd.Core.BasicType.ST_ID>(), new List<Ofd.Core.BasicType.ST_ID>());
        Assert.True(called);
    }
}
