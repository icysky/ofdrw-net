using Ofd.Core.PageDescription.Color.Color;
using Ofd.Core.Text.Text;
using Ofd.Font;
using FontType = Ofd.Font.Font;

namespace Ofd.Layout.Element.Canvas;

/// <summary>
/// 艺术字元素（Java ArtWord）：在 Span 效果基础上增加水平/垂直缩放、倾斜、偏移等艺术效果。
/// </summary>
public class ArtWord : CanvasBase
{
    /// <summary>字体。</summary>
    private FontType? _font;

    /// <summary>字体大小（默认 3mm）。</summary>
    private double? _fontSize = 3d;

    /// <summary>字间距（默认 0）。</summary>
    private double? _letterSpacing = 0d;

    /// <summary>是否加粗（等价 Weight=800）。</summary>
    private bool _bold;

    /// <summary>文字粗细。</summary>
    private Weight? _weight;

    /// <summary>是否斜体。</summary>
    private bool _italic;

    /// <summary>是否含下划线。</summary>
    private bool _underline;

    /// <summary>下划线与文字偏移量。</summary>
    private double _underlineOffset = 1.2d;

    /// <summary>下划线宽度（0 表示默认字号 0.05 倍）。</summary>
    private double _underlineWidth;

    /// <summary>文本内容。</summary>
    private string? _text;

    /// <summary>字体颜色（默认黑色）。</summary>
    private int[]? _color = { 0, 0, 0 };

    /// <summary>文本内容对齐方式（决定从何处开始绘制，非整体页面对齐）。</summary>
    private CanvasTextAlign? _textAlign;

    /// <summary>文本布局状态（相对画布的左右居中）。</summary>
    private TextAlign? _textLayoutAlign;

    /// <summary>水平缩放（&lt;1 挤压，&gt;1 拉伸）。</summary>
    private double _horizontalScaling = 1D;

    /// <summary>垂直缩放（&lt;1 挤压，&gt;1 拉伸）。</summary>
    private double _verticalScaling = 1D;

    /// <summary>水平倾斜（正数向右移动下边框）。</summary>
    private double _horizontalInclination;

    /// <summary>垂直倾斜（正数向下移动右边框）。</summary>
    private double _verticalInclination;

    /// <summary>水平偏移量（mm）。</summary>
    private double _offsetX;

    /// <summary>垂直偏移量（mm）。</summary>
    private double _offsetY;

    public ArtWord(double? width, double? height) : base(width, height) { }

    public int[]? GetColor() => _color;

    public ArtWord SetColor(int[]? rgb)
    {
        _color = rgb;
        return this;
    }

    public ArtWord SetColor(int r, int g, int b)
    {
        _color = new[] { r, g, b };
        return this;
    }

    /// <summary>设置字体颜色（16 进制字符串，如 #FFFFFF）。</summary>
    public ArtWord SetColor(string color)
    {
        if (color == null || color.Length < 7)
            throw new ArgumentException("颜色值格式错误：" + color);

        _color = new[]
        {
            Convert.ToInt32(color.Substring(1, 2), 16),
            Convert.ToInt32(color.Substring(3, 2), 16),
            Convert.ToInt32(color.Substring(5, 2), 16),
        };
        return this;
    }

    /// <summary>字符数量。</summary>
    public int Length() => _text?.Length ?? 0;

    public FontType? GetFont() => _font;

    public ArtWord SetFont(FontType? font)
    {
        _font = font;
        return this;
    }

    public double? GetFontSize() => _fontSize;

    public ArtWord SetFontSize(double? fontSize)
    {
        _fontSize = fontSize;
        return this;
    }

    public double? GetLetterSpacing() => _letterSpacing;

    public ArtWord SetLetterSpacing(double? letterSpacing)
    {
        _letterSpacing = letterSpacing;
        return this;
    }

    public bool IsBold() => _bold;

    public ArtWord SetBold(bool bold)
    {
        _bold = bold;
        return this;
    }

    public Weight? GetWeight() => _weight;

    public ArtWord SetWeight(Weight? weight)
    {
        _weight = weight;
        return this;
    }

    public bool IsItalic() => _italic;

    public ArtWord SetItalic(bool italic)
    {
        _italic = italic;
        return this;
    }

    public bool IsUnderline() => _underline;

    public ArtWord SetUnderline(bool underline)
    {
        _underline = underline;
        return this;
    }

    /// <summary>设置下划线（偏移量 mm，线宽 0 表示字号 0.05 倍）。</summary>
    public ArtWord SetUnderline(bool underline, double offset, double width)
    {
        _underline = underline;
        _underlineOffset = offset;
        _underlineWidth = width;
        return this;
    }

    public string? GetText() => _text;

    public ArtWord SetText(string? text)
    {
        _text = text;
        return this;
    }

    public double GetOffsetY() => _offsetY;

    public ArtWord SetOffsetY(double offsetY)
    {
        _offsetY = offsetY;
        return this;
    }

    public double GetOffsetX() => _offsetX;

    public ArtWord SetOffsetX(double offsetX)
    {
        _offsetX = offsetX;
        return this;
    }

    public double GetVerticalInclination() => _verticalInclination;

    public ArtWord SetVerticalInclination(double verticalInclination)
    {
        _verticalInclination = verticalInclination;
        return this;
    }

    public double GetHorizontalInclination() => _horizontalInclination;

    public ArtWord SetHorizontalInclination(double horizontalInclination)
    {
        _horizontalInclination = horizontalInclination;
        return this;
    }

    public double GetVerticalScaling() => _verticalScaling;

    public ArtWord SetVerticalScaling(double verticalScaling)
    {
        _verticalScaling = verticalScaling;
        return this;
    }

    public double GetHorizontalScaling() => _horizontalScaling;

    public ArtWord SetHorizontalScaling(double horizontalScaling)
    {
        _horizontalScaling = horizontalScaling;
        return this;
    }

    public CanvasTextAlign? GetTextAlign() => _textAlign;

    public ArtWord SetTextAlign(CanvasTextAlign? textAlign)
    {
        _textAlign = textAlign;
        return this;
    }

    public double GetUnderlineOffset() => _underlineOffset;

    public double GetUnderlineWidth() => _underlineWidth;

    public TextAlign? GetTextLayoutAlign() => _textLayoutAlign;

    public ArtWord SetTextLayoutAlign(TextAlign? textLayoutAlign)
    {
        _textLayoutAlign = textLayoutAlign;
        return this;
    }

    /// <summary>
    /// 克隆（属性一致的新对象）。
    /// <para>
    /// 注：Java 原实现未复制缩放/倾斜/偏移/对齐等艺术效果参数，克隆后艺术效果会丢失，此处补齐。
    /// </para>
    /// </summary>
    public ArtWord Clone()
    {
        var artWord = new ArtWord(GetWidth(), GetHeight())
        {
            _font = _font,
            _fontSize = _fontSize,
            _letterSpacing = _letterSpacing,
            _bold = _bold,
            _weight = _weight,
            _italic = _italic,
            _underline = _underline,
            _underlineOffset = _underlineOffset,
            _underlineWidth = _underlineWidth,
            _text = _text,
            _color = _color == null ? null : (int[])_color.Clone(),
            _horizontalScaling = _horizontalScaling,
            _verticalScaling = _verticalScaling,
            _horizontalInclination = _horizontalInclination,
            _verticalInclination = _verticalInclination,
            _offsetX = _offsetX,
            _offsetY = _offsetY,
            _textAlign = _textAlign,
            _textLayoutAlign = _textLayoutAlign,
        };
        return artWord;
    }

    /// <summary>绘制艺术字。</summary>
    public override void Draw(DrawContext ctx)
    {
        if (_text == null) return;

        // 字体设置
        var fontSetting = new FontSetting(_fontSize ?? 0, _font ?? Ofd.Font.FontName.SimSun.Font());
        fontSetting.SetLetterSpacing(_letterSpacing ?? 0);
        if (_bold) fontSetting.SetBold();
        if (_weight != null) fontSetting.SetFontWeight((int)_weight.Value);
        fontSetting.SetItalic(_italic);
        fontSetting.SetTextAlign(_textAlign);
        ctx.SetFont(fontSetting);

        // 字体颜色
        ctx.SetFillColor(_color ?? new[] { 0, 0, 0 });

        // a-水平缩放 b-水平倾斜 c-垂直倾斜 d-垂直缩放 e-水平偏移 f-垂直偏移
        ctx.SetTransform(_horizontalScaling, _verticalInclination, _horizontalInclination,
            _verticalScaling, _offsetX, _offsetY);

        // 文本宽度
        double textWidth = ctx.MeasureText(_text).Width ?? 0;

        // 按布局对齐计算起始 x
        double x = _textLayoutAlign switch
        {
            TextAlign.Right => (GetWidth() ?? 0) - textWidth,
            TextAlign.Center => ((GetWidth() ?? 0) - textWidth) / 2,
            _ => 0,
        };

        ctx.FillText(_text, x, _fontSize ?? 0);

        // 下划线
        if (_underline)
        {
            ctx.SetLineWidth(_underlineWidth == 0D ? (_fontSize ?? 0) * 0.05 : _underlineWidth);
            ctx.BeginPath();
            ctx.SetGlobalAlpha(1D);
            ctx.StrokeStyle = CT_Color.Rgb(_color ?? new[] { 0, 0, 0 });
            ctx.MoveTo(x, (_fontSize ?? 0) + _offsetY + _underlineOffset);
            ctx.LineTo(x + textWidth, (_fontSize ?? 0) + _offsetY + _underlineOffset);
            ctx.Stroke();
        }
    }
}
