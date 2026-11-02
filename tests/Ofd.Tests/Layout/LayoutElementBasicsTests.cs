using Ofd.Font;
using Ofd.Layout.Element;
using Ofd.Layout.Element.Canvas;
using Xunit;
using FontType = Ofd.Font.Font;

namespace Ofd.Tests.Layout;

/// <summary>排版元素基础类型：数组参数归一、字符块宽高、测量模型。</summary>
public class LayoutElementBasicsTests
{
    private sealed class MockTextFont : ITextFontInfo
    {
        private readonly FontType _font;
        private readonly double? _letterSpacing;
        private readonly double? _fontSize;

        public MockTextFont(FontType font, double? letterSpacing, double? fontSize)
        {
            _font = font;
            _letterSpacing = letterSpacing;
            _fontSize = fontSize;
        }

        public FontType GetFont() => _font;

        public double? GetLetterSpacing() => _letterSpacing;

        public double? GetFontSize() => _fontSize;
    }

    [Fact]
    public void Array_param_tool_normalizes_four_values()
    {
        Assert.Equal(new double[] { 0, 0, 0, 0 }, ArrayParamTool.Arr4p());
        Assert.Equal(new double[] { 0, 0, 0, 0 }, ArrayParamTool.Arr4p(Array.Empty<double>()));

        Assert.Equal(new double[] { 2, 2, 2, 2 }, ArrayParamTool.Arr4p(2));
        Assert.Equal(new double[] { 1, 2, 1, 2 }, ArrayParamTool.Arr4p(1, 2));
        Assert.Equal(new double[] { 1, 2, 3, 0 }, ArrayParamTool.Arr4p(1, 2, 3));
        Assert.Equal(new double[] { 1, 2, 3, 4 }, ArrayParamTool.Arr4p(1, 2, 3, 4));
        Assert.Equal(new double[] { 1, 2, 3, 4 }, ArrayParamTool.Arr4p(1, 2, 3, 4, 5));
    }

    [Fact]
    public void Enums_expose_java_compatible_members()
    {
        Assert.Equal(3, Enum.GetValues<AFloat>().Length);
        Assert.Equal(3, Enum.GetValues<Position>().Length);
        Assert.Equal(2, Enum.GetValues<Display>().Length);
        Assert.Equal(4, Enum.GetValues<Clear>().Length);
        Assert.Equal(3, Enum.GetValues<TextAlign>().Length);
        Assert.Equal(5, Enum.GetValues<CanvasTextAlign>().Length);
        Assert.Equal(3, Enum.GetValues<VerticalAlign>().Length);
    }

    [Fact]
    public void TxtGlyph_uses_width_map_when_font_has_one()
    {
        var font = Ofd.Font.FontName.TimesNewRoman.Font();
        var info = new MockTextFont(font, 0.5, 10);

        var glyph = new TxtGlyph('A', info);

        double expected = 0.5 + 10 * font.GetCharWidthScale('A');
        Assert.Equal(expected, glyph.GetW(), 6);

        // 有映射表时高度直接取字号
        Assert.Equal(10, glyph.GetH(), 6);
    }

    [Fact]
    public void TxtGlyph_measures_font_without_map()
    {
        var font = Ofd.Font.FontName.SimSun.Font();
        var info = new MockTextFont(font, 1, 12);

        var glyph = new TxtGlyph('中', info);
        double w = glyph.GetW();
        double h = glyph.GetH();

        Assert.True(w > 1); // 字间距 + 实测宽度
        Assert.True(h > 0);

        // 缓存后重复取值稳定
        Assert.Equal(w, glyph.GetW());
        Assert.Equal(h, glyph.GetH());
    }

    [Fact]
    public void MeasureBody_sums_absolute_offsets()
    {
        var body = new MeasureBody { Offset = new double[] { 1, -2, 0.5 } };
        body.With(3);

        Assert.Equal(6.5, body.Width, 6);
        Assert.Equal(0, body.FirstCharOffsetX);
        Assert.Equal(0, body.FirstCharOffsetY);
    }

    [Fact]
    public void Text_metrics_models_have_defaults()
    {
        var metrics = new TextMetrics();
        Assert.Null(metrics.Width);
        Assert.Null(metrics.FontSize);
        Assert.Null(metrics.Offset);
        Assert.Equal(0, metrics.ReadDirection);

        var area = new TextMetricsArea();
        Assert.Null(area.CharAreas);
        Assert.Equal(0, area.Width);
        Assert.Equal(0, area.Height);
        Assert.Equal(0, area.LetterSpacing);
    }
}
