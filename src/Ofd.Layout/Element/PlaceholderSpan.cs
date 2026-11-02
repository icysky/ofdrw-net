namespace Ofd.Layout.Element;

/// <summary>
/// 占位符（Java PlaceholderSpan）：占有指定宽度/高度但不绘制文字。
/// <para>holdWidth 与 holdNum 仅一个有效，holdWidth 优先级更高。</para>
/// </summary>
public class PlaceholderSpan : Span
{
    /// <summary>占位数量。</summary>
    private int _holdNum;

    /// <summary>占位符宽度（mm）。</summary>
    private double _holdWidth;

    /// <summary>以占位宽度与高度创建占位符。</summary>
    public PlaceholderSpan(double holdWidth, double height)
    {
        _holdWidth = holdWidth;
        SetFontSize(height);
        SetIntegrity(true); // 不可再分割
    }

    /// <summary>以占位数量与每个占位字号创建占位符（字间距为 0）。</summary>
    public PlaceholderSpan(int holdNum, double fontSize)
    {
        SetFontSize(fontSize);
        SetIntegrity(true);
        SetHoldChars(holdNum);
    }

    /// <summary>通过复制已有 Span 创建占位符。</summary>
    public PlaceholderSpan(int holdNum, Span sp)
    {
        SetIntegrity(true);
        SetFontSize(sp.GetFontSize());
        SetLetterSpacing(sp.GetLetterSpacing());
        SetHoldChars(holdNum);
    }

    /// <summary>设置占位字符数量（同时推导占位宽度）。</summary>
    public PlaceholderSpan SetHoldChars(int holdNum)
    {
        _holdNum = holdNum;
        _holdWidth = holdNum * ((GetFontSize() ?? 0) + (GetLetterSpacing() ?? 0));
        return this;
    }

    /// <summary>获取占位数量（仅数量方式创建时有效）。</summary>
    public int GetHoldNum() => _holdNum;

    /// <summary>占位符宽度（仅宽度方式创建时有效）。</summary>
    public double GetHoldWidth() => _holdWidth;

    /// <summary>设置占位符宽度（设置后 HoldNum 失效）。</summary>
    public PlaceholderSpan SetHoldWidth(double holdWidth)
    {
        _holdWidth = holdWidth;
        return this;
    }

    /// <summary>占位符大小：宽度取 holdWidth，高度取字号。</summary>
    public override Rectangle BlockSize() => new(_holdWidth, GetFontSize() ?? 0);

    /// <summary>占位符不参与换行切分。</summary>
    public override LinkedList<Span> SplitLineBreak()
    {
        var res = new LinkedList<Span>();
        res.AddLast(this);
        return res;
    }
}
