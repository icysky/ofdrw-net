using System.Text;
using Ofd.Core.Dom;
using Ofd.Layout.Element.Canvas;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>颜色名解析与 Canvas 渐变容器。</summary>
public class CanvasColorTests
{
    [Fact]
    public void Named_colors_resolve_case_insensitively()
    {
        Assert.Equal(new[] { 255, 0, 0 }, NamedColor.Rgb("red"));
        Assert.Equal(new[] { 255, 0, 0 }, NamedColor.Rgb("RED"));
        Assert.Equal(new[] { 255, 0, 0 }, NamedColor.Rgb("  Red  "));
        Assert.Equal(new[] { 0, 0, 0 }, NamedColor.Rgb("black"));
        Assert.Equal(new[] { 255, 255, 255 }, NamedColor.Rgb("white"));

        // Java 表中的非标准颜色名
        Assert.Equal(new[] { 209, 146, 117 }, NamedColor.Rgb("feldspar"));
        Assert.Equal(new[] { 208, 32, 144 }, NamedColor.Rgb("violetred"));
    }

    [Fact]
    public void Hex_and_functional_formats_parse()
    {
        Assert.Equal(new[] { 255, 255, 255 }, NamedColor.Rgb("#fff"));
        Assert.Equal(new[] { 255, 0, 0 }, NamedColor.Rgb("#FF0000"));
        Assert.Equal(new[] { 0, 255, 0 }, NamedColor.Rgb("0x00FF00"));

        Assert.Equal(new[] { 1, 2, 3 }, NamedColor.Rgb("rgb(1, 2, 3)"));
        Assert.Equal(new[] { 1, 2, 3 }, NamedColor.Rgb("rgb(1,2,3)"));

        // rgba 透明度 0.5 → 127
        Assert.Equal(new[] { 1, 2, 3, 127 }, NamedColor.Rgb("rgba(1, 2, 3, 0.5)"));
        Assert.Equal(new[] { 1, 2, 3, 255 }, NamedColor.Rgb("rgba(1,2,3,1)"));
    }

    [Fact]
    public void Invalid_colors_return_null()
    {
        Assert.Null(NamedColor.Rgb(null));
        Assert.Null(NamedColor.Rgb(""));
        Assert.Null(NamedColor.Rgb("not-a-color"));
        Assert.Null(NamedColor.Rgb("#ff"));
        Assert.Null(NamedColor.Rgb("rgb(a,b,c)"));
    }

    [Fact]
    public void Linear_gradient_collects_color_stops()
    {
        var g = new CanvasGradient(0, 0, 100, 0);
        g.AddColorStop(0, "#FF0000");
        g.AddColorStop(0.5, "rgba(0, 255, 0, 0.5)");
        g.AddColorStop(1, "not-a-color"); // 忽略无效颜色

        var segments = g.AxialShd.GetSegments();
        Assert.Equal(2, segments.Count);

        var xml = Encoding.UTF8.GetString(DomSerializer.Dump(g.AxialShd.GetProxy()));
        Assert.Contains("StartPoint=\"0 0\"", xml);
        Assert.Contains("EndPoint=\"100 0\"", xml);

        // 第二段包含透明度属性
        Assert.Equal(127, segments[1].GetColor()!.GetAlpha());
    }

    [Fact]
    public void Radial_gradient_sets_geometry_and_extend()
    {
        var g = new CanvasRadialGradient(10, 20, 5, 10, 20, 15);
        g.AddColorStop(0, "blue");
        g.AddColorStop(1, "white");

        var shd = g.RadialShd;
        Assert.Equal(5, double.Parse(radialAttr(shd, "StartRadius")));
        Assert.Equal(15, double.Parse(radialAttr(shd, "EndRadius")));
        Assert.Equal("1", radialAttr(shd, "Extend"));
        Assert.Equal(2, shd.GetSegments().Count);
    }

    private static string radialAttr(Ofd.Core.PageDescription.Color.Color.CT_RadialShd shd, string name) =>
        shd.GetProxy().Attr(name) ?? "";
}
