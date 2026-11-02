namespace Ofd.Layout.Element;

/// <summary>
/// 行内文字（Java TxtLineBlock）：一行内可容纳的 Span 集合与尺寸统计。
/// </summary>
public class TxtLineBlock
{
    /// <summary>行内文字对齐方式（默认左对齐）。</summary>
    private readonly TextAlign _textAlign;

    /// <summary>行内元素。</summary>
    private readonly List<Span> _inlineSpans;

    /// <summary>最高文字高度。</summary>
    private double _maxSpanHeight;

    /// <summary>行宽度。</summary>
    private double _width;

    /// <summary>行间距。</summary>
    private readonly double? _lineSpace;

    /// <summary>行内可用最大宽度。</summary>
    private readonly double _lineMaxAvailableWidth;

    public TxtLineBlock(double lineMaxAvailableWidth, double? lineSpace) :
        this(lineMaxAvailableWidth, lineSpace, TextAlign.Left) { }

    public TxtLineBlock(double lineMaxAvailableWidth, double? lineSpace, TextAlign textAlign)
    {
        _lineMaxAvailableWidth = lineMaxAvailableWidth;
        _lineSpace = lineSpace;
        _textAlign = textAlign;
        _inlineSpans = new List<Span>();
    }

    /// <summary>尝试加入文字单元：空间足够返回 true 并加入。</summary>
    public bool TryAdd(Span span)
    {
        var rec = span.BlockSize();
        if (rec.GetWidth() + _width > _lineMaxAvailableWidth)
            return false;

        _width += rec.GetWidth();
        if (rec.GetHeight() > _maxSpanHeight) _maxSpanHeight = rec.GetHeight();

        _inlineSpans.Add(span);
        return true;
    }

    /// <summary>
    /// 尝试切分加入：返回切分后剩余部分；行空间耗尽返回 null。
    /// </summary>
    public Span? TrySplitAdd(Span span)
    {
        if (span.IsIntegrity() == true)
            throw new InvalidOperationException("文字单元（Span）不可拆分");

        double remainWidth = _lineMaxAvailableWidth - _width;
        int splitIndex = 0;

        var glyphs = span.GlyphList();
        for (int i = 0; i < glyphs.Count; i++)
        {
            var glyph = glyphs[i];
            if (glyph.GetW() <= remainWidth)
            {
                remainWidth -= glyph.GetW();
                continue;
            }

            // 第一个字符都放不下 → 行空间耗尽
            if (i == 0) return null;

            splitIndex = i;
            break;
        }

        var split = span.Split(splitIndex);
        TryAdd(split[0]);   // 前半段入行
        return split[1];    // 后半段留待下一行
    }

    public bool IsEmpty() => _inlineSpans.Count == 0;

    public List<Span> GetInlineSpans() => _inlineSpans;

    /// <summary>行占据区域（宽 × (最高字高 + 行间距)）。</summary>
    public Rectangle Size() => new(_width, _maxSpanHeight + (_lineSpace ?? 0));

    /// <summary>行占据高度（文字高度 + 行间距）。</summary>
    public double GetHeight() => _maxSpanHeight + (_lineSpace ?? 0);

    public double? GetLineSpace() => _lineSpace;

    public double GetMaxSpanHeight() => _maxSpanHeight;

    public double GetWidth() => _width;

    public TextAlign GetTextAlign() => _textAlign;

    public double GetLineMaxAvailableWidth() => _lineMaxAvailableWidth;
}
