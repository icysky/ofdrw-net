namespace Ofd.Layout.Element.Canvas;

/// <summary>测量的文字信息（Java TextMetrics）。</summary>
public class TextMetrics
{
    /// <summary>阅读方向。</summary>
    public int ReadDirection;

    /// <summary>
    /// 文字宽度（毫米）。readDirection 为 0/180 时为宽度，90/270 时为高度。
    /// </summary>
    public double? Width;

    /// <summary>文本字体大小（毫米）。</summary>
    public double? FontSize;

    /// <summary>后一个字对前一个字的偏移量。</summary>
    public double[]? Offset;
}
