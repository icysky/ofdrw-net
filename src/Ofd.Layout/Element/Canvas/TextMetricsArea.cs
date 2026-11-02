using Ofd.Font;

namespace Ofd.Layout.Element.Canvas;

/// <summary>
/// 测量的文字区域信息（Java TextMetricsArea）。
/// <para>宽度 = 每个字宽度 + 字间距 × (n - 1)。</para>
/// </summary>
public class TextMetricsArea
{
    /// <summary>每个字符大小。</summary>
    public List<TextBounds>? CharAreas;

    /// <summary>文字区域宽度（毫米）。</summary>
    public double Width;

    /// <summary>文字区域高度（毫米）。</summary>
    public double Height;

    /// <summary>字间距。</summary>
    public double LetterSpacing;
}
