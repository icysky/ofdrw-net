using Ofd.Core.Text.Text;
using Ofd.Layout.Element.Canvas;
using FontType = Ofd.Font.Font;

namespace Ofd.Layout.Element;

/// <summary>
/// 字体基础单元（Java Span）：字体、字号、字间距、粗细、斜体、下划线、颜色等。
/// </summary>
public class Span : ITextFontInfo
{
    /// <summary>字体。</summary>
    private FontType? _font;

    /// <summary>字体大小（默认 3mm）。</summary>
    private double? _fontSize = 3d;

    /// <summary>字间距（默认 0）。</summary>
    private double? _letterSpacing = 0d;

    /// <summary>是否加粗（等价于 Weight=W_800）。</summary>
    private bool _bold;

    /// <summary>文字粗细（默认 null）。</summary>
    private Weight? _weight;

    /// <summary>是否斜体。</summary>
    private bool _italic;

    /// <summary>是否含下划线。</summary>
    private bool _underline;

    /// <summary>下划线与文字偏移量。</summary>
    private double _underlineOffset = 1.2d;

    /// <summary>下划线宽度（0 表示默认 = 字号的 0.05 倍）。</summary>
    private double _underlineWidth;

    /// <summary>是否填充（默认 true）。</summary>
    private bool _fill = true;

    /// <summary>文本内容。</summary>
    private string? _text;

    /// <summary>字体颜色。</summary>
    private int[]? _fillColor;

    /// <summary>是否占满剩余行空间。</summary>
    private bool _linebreak;

    /// <summary>是否不可拆分（默认 false）。</summary>
    private bool? _integrity = false;

    /// <summary>字符块缓存。</summary>
    private List<TxtGlyph>? _txtGlyphsCache;

    protected Span()
    {
        SetFont(FontType.GetDefault());
    }

    public Span(FontType font, double? fontSize, string? text)
    {
        _font = font;
        _fontSize = fontSize;
        SetText(text);
    }

    public Span(string text) : this()
    {
        if (text == null) throw new ArgumentException("text内容为空");
        SetText(text);
    }

    public int[]? GetColor() => _fillColor;

    /// <summary>设置字体颜色（RGB 数组）。</summary>
    public Span SetColor(int[] rgb)
    {
        _fillColor = rgb;
        return this;
    }

    /// <summary>设置文字颜色（支持 16 进制 / rgb() / 颜色名）。</summary>
    public Span SetColor(string color)
    {
        int[]? c = NamedColor.Rgb(color);
        if (c == null || c.Length < 3) throw new ArgumentException("边框颜色 颜色值错误：" + color);
        return SetColor(c);
    }

    /// <summary>设置字体颜色 RGB。</summary>
    public Span SetColor(int r, int g, int b)
    {
        _fillColor = new[] { r, g, b };
        return this;
    }

    /// <summary>字符数量。</summary>
    public int Length() => _text?.Length ?? 0;

    public FontType GetFont() => _font ?? FontType.GetDefault();

    public Span SetFont(FontType font)
    {
        _font = font;
        return this;
    }

    public double? GetFontSize() => _fontSize;

    public Span SetFontSize(double? fontSize)
    {
        _fontSize = fontSize;
        return this;
    }

    public double? GetLetterSpacing() => _letterSpacing;

    public Span SetLetterSpacing(double? letterSpacing)
    {
        _letterSpacing = letterSpacing;
        return this;
    }

    public bool IsBold() => _bold;

    /// <summary>设置是否加粗（更细粒度控制用 SetWeight）。</summary>
    public Span SetBold(bool bold)
    {
        _bold = bold;
        return this;
    }

    public Weight? GetWeight() => _weight;

    public Span SetWeight(Weight? weight)
    {
        _weight = weight;
        return this;
    }

    public bool IsItalic() => _italic;

    public Span SetItalic(bool italic)
    {
        _italic = italic;
        return this;
    }

    public bool IsUnderline() => _underline;

    public Span SetUnderline(bool underline)
    {
        _underline = underline;
        return this;
    }

    /// <summary>设置下划线（偏移量单位 mm，默认 1.2；线宽 0 表示字号 0.05 倍）。</summary>
    public Span SetUnderline(bool underline, double offset, double width)
    {
        _underline = underline;
        _underlineOffset = offset;
        _underlineWidth = width;
        return this;
    }

    public bool? IsFill() => _fill;

    public Span SetFill(bool fill)
    {
        _fill = fill;
        return this;
    }

    public string? GetText() => _text;

    public Span SetText(string? text)
    {
        _text = text;
        // 已有缓存时重建。
        // 注：Java 原实现只调用了 glyphList()，而该方法在缓存非空时直接返回，
        // 导致改写文本后字符块宽度仍然是旧值（上游缺陷），此处先置空缓存再重建。
        if (_txtGlyphsCache != null)
        {
            _txtGlyphsCache = null;
            GlyphList();
        }
        return this;
    }

    /// <summary>
    /// 元素是否不可拆分。特殊：无文字或只有一个文字时，无论设置如何都不可拆分。
    /// </summary>
    public bool? IsIntegrity()
    {
        if (_text == null || _text.Length <= 1) _integrity = true;
        return _integrity;
    }

    public Span SetIntegrity(bool? integrity)
    {
        _integrity = integrity;
        return this;
    }

    /// <summary>获取字体图形列表（缓存）。</summary>
    public List<TxtGlyph> GlyphList()
    {
        if (_txtGlyphsCache == null)
        {
            _txtGlyphsCache = new List<TxtGlyph>(_text?.Length ?? 0);
            foreach (char c in _text ?? "")
                _txtGlyphsCache.Add(new TxtGlyph(c, this));
        }
        return _txtGlyphsCache;
    }

    /// <summary>字符 X 坐标偏移队列（每个值为后一字符相对前一字符的偏移，长度 = 字符数-1）。</summary>
    public double[] GetDeltaX()
    {
        var list = GlyphList();
        int len = list.Count;
        if (len <= 1) return Array.Empty<double>();

        var res = new double[len - 1];
        for (int i = 0; i < len - 1; i++)
            res[i] = list[i].GetW();
        return res;
    }

    /// <summary>不拆分时放在一行内占用的大小。</summary>
    public virtual Rectangle BlockSize()
    {
        double width = 0;
        double height = 0;
        foreach (var glyph in GlyphList())
        {
            width += glyph.GetW();
            if (glyph.GetH() > height) height = glyph.GetH();
        }
        return new Rectangle(width, height);
    }

    /// <summary>按字符坐标切分为两个全新元素。</summary>
    public Span[] Split(int index)
    {
        string text = _text ?? "";
        if (index < 0 || index >= text.Length)
            throw new ArgumentException("非法的切分数组坐标(index): " + index);

        var s1 = Clone().SetText(text[..index]);
        var s2 = Clone().SetText(text[index..]);
        return new[] { s1, s2 };
    }

    /// <summary>设置为占满剩余行空间（等价于末尾追加 \n）。</summary>
    public Span SetLinebreak(bool linebreak)
    {
        _linebreak = linebreak;
        return this;
    }

    /// <summary>是否是占满剩余行空间的 Span。</summary>
    public bool HasLinebreak() => _linebreak;

    /// <summary>按行内换行处理后的 Span 列表。</summary>
    public virtual LinkedList<Span> SplitLineBreak()
    {
        var res = new LinkedList<Span>();

        if (_text == null || !_text.Contains('\n'))
        {
            res.AddLast(this);
            return res;
        }

        // 与 Java String.split("\n") 一致：丢弃末尾空串
        string[] split = _text.Split('\n');
        for (int i = 0; i < split.Length; i++)
        {
            var lineSpan = Clone().SetText(split[i]);
            if (i != split.Length - 1) lineSpan.SetLinebreak(true);
            else if (_text.EndsWith('\n')) lineSpan.SetLinebreak(true);

            res.AddLast(lineSpan);
        }
        return res;
    }

    /// <summary>下划线与文字的偏移量（mm）。</summary>
    public double GetUnderlineOffset() => _underlineOffset;

    /// <summary>下划线宽度（0 表示默认字号 0.05 倍，mm）。</summary>
    public double GetUnderlineWidth() => _underlineWidth;

    public virtual Span Clone()
    {
        var span = new Span
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
            _fill = _fill,
            _text = _text,
            _integrity = _integrity,
            _linebreak = _linebreak,
            _fillColor = _fillColor == null ? null : (int[])_fillColor.Clone(),
        };
        return span;
    }
}
