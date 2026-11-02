namespace Ofd.Layout.Element.Canvas;

/// <summary>文本测量分析结果（Java MeasureBody）。</summary>
public class MeasureBody
{
    /// <summary>文本字符间相对偏移量。</summary>
    public double[] Offset = Array.Empty<double>();

    /// <summary>文本在阅读方向上的总宽度。</summary>
    public double Width;

    /// <summary>文本第一个字符相对偏移 X。</summary>
    public double FirstCharOffsetX;

    /// <summary>文本第一个字符相对偏移 Y。</summary>
    public double FirstCharOffsetY;

    /// <summary>加上偏移量后的宽度：Σ|offset| + charLen。</summary>
    public void With(double charLen) => Width = Offset.Sum(o => Math.Abs(o)) + charLen;
}
