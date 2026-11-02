using FontType = Ofd.Font.Font;

namespace Ofd.Layout.Element;

/// <summary>
/// 段落（Java Paragraph）：由若干 <see cref="Span"/> 组成的文本块，按宽度自动切分为行。
/// </summary>
public class Paragraph : Div
{
    /// <summary>首行缩进字符数（null 表示没有缩进）。</summary>
    private int? _firstLineIndent;

    /// <summary>首行缩进数值（mm）。</summary>
    private double? _firstLineIndentWidth;

    /// <summary>行间距（默认 2mm）。</summary>
    private double? _lineSpace = 2d;

    /// <summary>默认字体。</summary>
    private FontType? _defaultFont;

    /// <summary>默认字号。</summary>
    private double? _defaultFontSize;

    /// <summary>文字内容。</summary>
    private LinkedList<Span> _contents;

    /// <summary>元素内行缓存。</summary>
    private LinkedList<TxtLineBlock> _lines;

    /// <summary>字体在段落内的对齐方式（默认左对齐）。</summary>
    private TextAlign _textAlign = TextAlign.Left;

    /// <summary>创建固定大小段落。</summary>
    public Paragraph(double? width, double? height) : this()
    {
        SetWidth(width).SetHeight(height);
    }

    /// <summary>创建段落（未设置宽度时独占整段）。</summary>
    public Paragraph()
    {
        SetClear(Clear.Both);
        _contents = new LinkedList<Span>();
        _lines = new LinkedList<TxtLineBlock>();
    }

    /// <summary>创建绝对定位段落。</summary>
    public Paragraph(double x, double y, double width, double height) : base(x, y, width, height)
    {
        _contents = new LinkedList<Span>();
        _lines = new LinkedList<TxtLineBlock>();
    }

    /// <summary>创建绝对定位段落并指定文本与字号。</summary>
    public Paragraph(double x, double y, double width, double height, string text, double fontSize)
        : base(x, y, width, height)
    {
        if (text == null) throw new ArgumentException("文字内容为null");
        _contents = new LinkedList<Span>();
        _lines = new LinkedList<TxtLineBlock>();

        SetFontSize(fontSize);
        Add(text);
    }

    public Paragraph(string text) : this()
    {
        if (text == null) throw new ArgumentException("文字内容为null");
        Add(text);
    }

    public Paragraph(string text, double? defaultFontSize) : this()
    {
        if (text == null) throw new ArgumentException("文字内容为null");
        SetFontSize(defaultFontSize);
        Add(text);
    }

    public Paragraph(string text, double? defaultFontSize, FontType? defaultFont) : this()
    {
        if (text == null) throw new ArgumentException("文字内容为null");
        SetFontSize(defaultFontSize);
        SetDefaultFont(defaultFont);
        Add(text);
    }

    public Paragraph(string text, FontType? defaultFont) : this()
    {
        if (text == null) throw new ArgumentException("文字内容为null");
        SetDefaultFont(defaultFont);
        Add(text);
    }

    /// <summary>增加段落文字（使用段落默认字体样式）。</summary>
    public Paragraph Add(string? text)
    {
        if (text == null) return this;

        var newTxt = new Span(text);
        if (_defaultFont != null) newTxt.SetFont(_defaultFont);
        if (_defaultFontSize != null) newTxt.SetFontSize(_defaultFontSize);

        return Add(newTxt);
    }

    /// <summary>加入带有特殊样式的文字内容。</summary>
    public Paragraph Add(Span? content)
    {
        if (content == null) return this;
        _contents.AddLast(content);
        return this;
    }

    public double? GetLineSpace() => _lineSpace;

    public Paragraph SetLineSpace(double? lineSpace)
    {
        _lineSpace = lineSpace;
        return this;
    }

    public FontType? GetDefaultFont() => _defaultFont;

    public Paragraph SetDefaultFont(FontType? defaultFont)
    {
        _defaultFont = defaultFont;
        return this;
    }

    /// <summary>设置默认字体，可选择对已加入内容生效。</summary>
    public Paragraph SetDefaultFont(FontType? defaultFont, bool refreshBeforeAdd)
    {
        _defaultFont = defaultFont;
        if (refreshBeforeAdd)
        {
            foreach (var span in _contents) span.SetFont(defaultFont ?? FontType.GetDefault());
        }
        return this;
    }

    public double? GetFontSize() => _defaultFontSize;

    /// <summary>设置段落默认字号（不影响已加入内容）。</summary>
    public Paragraph SetFontSize(double? defaultFontSize)
    {
        _defaultFontSize = defaultFontSize;
        return this;
    }

    /// <summary>设置段落默认字号，可选择对已加入内容生效。</summary>
    public Paragraph SetFontSize(double? defaultFontSize, bool refreshBeforeAdd)
    {
        _defaultFontSize = defaultFontSize;
        if (refreshBeforeAdd)
        {
            foreach (var span in _contents) span.SetFontSize(defaultFontSize);
        }
        return this;
    }

    public LinkedList<Span> GetContents() => _contents;

    public Paragraph SetContents(IEnumerable<Span> contents)
    {
        _contents.Clear();
        foreach (var span in contents) _contents.AddLast(span);
        return this;
    }

    public LinkedList<TxtLineBlock> GetLines() => _lines;

    private TxtLineBlock NewLine(double width) => new(width, _lineSpace, _textAlign);

    public int? GetFirstLineIndent() => _firstLineIndent;

    /// <summary>设置首行缩进字符数（null 或 0 表示不缩进）。</summary>
    public Paragraph SetFirstLineIndent(int? firstLineIndent)
    {
        _firstLineIndent = firstLineIndent;
        return this;
    }

    public double? GetFirstLineIndentWidth() => _firstLineIndentWidth;

    /// <summary>设置首行缩进数值（mm）。</summary>
    public Paragraph SetFirstLineIndentWidth(double firstLineIndentWidth)
    {
        _firstLineIndentWidth = firstLineIndentWidth;
        return this;
    }

    /// <summary>清除缩进格式。</summary>
    public Paragraph ClearFirstLineIndent()
    {
        _firstLineIndent = null;
        return this;
    }

    /// <summary>设置段落内文字对齐方式（默认左对齐）。</summary>
    public Paragraph SetTextAlign(TextAlign? textAlign)
    {
        _textAlign = textAlign ?? TextAlign.Left;
        return this;
    }

    public TextAlign GetTextAlign() => _textAlign;

    /// <summary>把 Span 内含的换行符拆成多个占满剩余行空间的 Span。</summary>
    private static LinkedList<Span> SpanLinebreakSplit(LinkedList<Span> seq)
    {
        var sps = new LinkedList<Span>();
        if (seq == null || seq.Count == 0) return sps;

        while (seq.Count > 0)
        {
            var pop = seq.First!.Value;
            seq.RemoveFirst();

            foreach (var span in pop.SplitLineBreak())
                sps.AddLast(span);
        }
        return sps;
    }

    /// <summary>处理首行缩进占位符。</summary>
    private void ProcessPlaceholder(LinkedList<Span> seq)
    {
        if (seq == null || seq.Count == 0) return;

        // 不需要占位符
        if ((_firstLineIndent == null || _firstLineIndent == 0)
            && (_firstLineIndentWidth == null || _firstLineIndentWidth == 0))
            return;

        var firstSpan = seq.First!.Value;
        if (firstSpan is PlaceholderSpan holder)
        {
            // 已存在缩进占位符但缩进被清零 → 移除
            if (holder.GetHoldWidth() == 0 && holder.GetHoldNum() == 0) seq.RemoveFirst();

            if (_firstLineIndentWidth != null && _firstLineIndentWidth > 0)
                holder.SetHoldWidth(_firstLineIndentWidth.Value);
            else
                holder.SetHoldChars(_firstLineIndent ?? 0);
            return;
        }

        if (_firstLineIndentWidth != null)
            seq.AddFirst(new PlaceholderSpan(_firstLineIndentWidth.Value, firstSpan.GetFontSize() ?? 0));
        else
            seq.AddFirst(new PlaceholderSpan(_firstLineIndent ?? 0, firstSpan));
    }

    private void SetHeightIfNotExist(double height)
    {
        if (GetHeight() == null || GetHeight() == 0) SetHeight(height);
    }

    private void SetWidthIfNotExist(double width)
    {
        if (GetWidth() == null || GetWidth() == 0) SetWidth(width);
    }

    /// <summary>预布局：按宽度切分行并计算尺寸（由渲染器调用）。</summary>
    public override Rectangle DoPrepare(double? widthLimit)
    {
        _lines.Clear();
        double? originW = GetWidth();
        double? lineMaxAvailableWidth = GetWidth();

        if (widthLimit == null) throw new ArgumentNullException(nameof(widthLimit));

        double limit = widthLimit.Value - WidthPlus();
        if (lineMaxAvailableWidth == null || lineMaxAvailableWidth > limit)
            lineMaxAvailableWidth = limit;

        var line = NewLine(lineMaxAvailableWidth!.Value);
        var seq = new LinkedList<Span>(_contents);

        // 首行缩进占位符
        ProcessPlaceholder(seq);
        // 换行符拆分
        seq = SpanLinebreakSplit(seq);

        while (seq.Count > 0)
        {
            var s = seq.First!.Value;
            seq.RemoveFirst();

            double blockWidth = s.BlockSize().GetWidth();
            // 不可分割元素超过行宽 → 忽略
            if (blockWidth > lineMaxAvailableWidth && s.IsIntegrity() == true) continue;

            // 可分割但第一个字符就超过可用宽度 → 丢弃
            if (s.IsIntegrity() != true && lineMaxAvailableWidth < s.GlyphList()[0].GetW()) continue;

            bool added = line.TryAdd(s);
            if (added)
            {
                // 占满剩余行空间 → 新起一行
                if (s.HasLinebreak())
                {
                    _lines.AddLast(line);
                    line = NewLine(lineMaxAvailableWidth.Value);
                }
                continue;
            }

            // 无法加入且不可分割 → 换行并重新入队
            if (s.IsIntegrity() == true)
            {
                _lines.AddLast(line);
                line = NewLine(lineMaxAvailableWidth.Value);
                seq.AddFirst(s);
                continue;
            }

            // 可分割 → 切分填充本行
            var toNextLineSpan = line.TrySplitAdd(s);
            if (toNextLineSpan == null) seq.AddFirst(s);      // 行空间耗尽
            else seq.AddFirst(toNextLineSpan);                 // 剩余部分入队

            _lines.AddLast(line);
            line = NewLine(lineMaxAvailableWidth.Value);
        }

        if (!line.IsEmpty()) _lines.AddLast(line);

        // 合并行高（+0.0001 抵消浮点误差）
        double height = 0;
        foreach (var txtLineBlock in _lines) height += txtLineBlock.GetHeight();
        height += 0.0001;

        SetHeightIfNotExist(height);

        // 宽度：null → 最长行宽；否则取 min(宽度, 限制)
        if (originW == null)
        {
            double maxWidth = _lines.Count > 0 ? _lines.Max(l => l.GetWidth()) : 0;
            SetWidth(maxWidth);
        }
        else
        {
            SetWidth(Math.Min(GetWidth() ?? 0, limit));
        }

        return new Rectangle((GetWidth() ?? 0) + WidthPlus(), (GetHeight() ?? 0) + HeightPlus());
    }

    /// <summary>设置行序列，并把行内文字单元回填到内容。</summary>
    private Paragraph SetLines(LinkedList<TxtLineBlock> lines)
    {
        _lines = lines;
        foreach (var item in lines)
        {
            foreach (var span in item.GetInlineSpans()) _contents.AddLast(span);
        }
        return this;
    }

    /// <summary>按行切分段落（内容分割调整）。</summary>
    public override Div[] ContentSplitAdjust(double sHeight, Div div1, Div div2)
    {
        var seq2 = new LinkedList<TxtLineBlock>(_lines);
        var seq1 = new LinkedList<TxtLineBlock>();

        double availableHeight = sHeight - GetMarginTop() - GetBorderTop() - GetPaddingTop();
        double remainHeight = availableHeight;

        while (seq2.Count > 0)
        {
            var line = seq2.First!.Value;
            if (remainHeight < line.GetHeight())
            {
                break; // 空间不足，保留在 seq2
            }

            seq2.RemoveFirst();
            seq1.AddLast(line);
            remainHeight -= line.GetHeight();
        }

        var p1 = (Paragraph)div1;
        var p2 = (Paragraph)div2;

        // seq2 为空：所有行都装得下
        // 注：此处必须把 p1 的高度收缩到实际可用空间。否则 p1 会保留克隆前的固定高度
        // （例如预设 210mm 而本页只剩 36mm），布局分析器会反复切分同一高度而不推进，
        // 表现为无限循环 + 内存暴涨。
        if (seq2.Count == 0)
        {
            p1.SetLines(seq1)
                .SetMarginBottom(0d).SetBorderBottom(0d).SetPaddingBottom(0d)
                .SetHeight(Math.Max(availableHeight, 0d));
            p2.SetHeight(0d).SetMarginTop(0d).SetBorderTop(0d).SetPaddingTop(0d);
            return new Div[] { p1, p2 };
        }

        // 一行都放不下：整体挪到下一段，本段用占位符顶上
        if (seq1.Count == 0)
        {
            var placeholder = Placeholder((GetWidth() ?? 0) + WidthPlus(), sHeight, GetFloat());
            return new Div[] { placeholder, this };
        }

        p1.SetLines(seq1)
            .SetMarginBottom(0d).SetBorderBottom(0d).SetPaddingBottom(0d)
            .SetHeight(availableHeight);

        p2.SetLines(seq2)
            .SetMarginTop(0d).SetBorderTop(0d).SetPaddingTop(0d)
            .SetHeight(seq2.Sum(l => l.GetHeight()));

        return new Div[] { p1, p2 };
    }

    /// <summary>
    /// 克隆：属性一致但无内容与行缓存。
    /// <para>
    /// 注：Java 原实现未复制 textAlign/firstLineIndent/firstLineIndentWidth，
    /// 导致分页切分后的段落丢失对齐与缩进，此处补齐。
    /// </para>
    /// </summary>
    public override Div Clone()
    {
        var p = (Paragraph)CopyTo(new Paragraph());
        p._lineSpace = _lineSpace;
        p._defaultFont = _defaultFont;
        p._defaultFontSize = _defaultFontSize;
        p._textAlign = _textAlign;
        p._firstLineIndent = _firstLineIndent;
        p._firstLineIndentWidth = _firstLineIndentWidth;
        p._lines = new LinkedList<TxtLineBlock>();
        p._contents = new LinkedList<Span>();
        return p;
    }

    public override string ElementType() => "Paragraph";
}
