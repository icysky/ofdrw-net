using Ofd.Font;
using FontType = Ofd.Font.Font;

namespace Ofd.Layout.Element;

/// <summary>文本字体信息（Java TextFontInfo）。</summary>
public interface ITextFontInfo
{
    /// <summary>获取字体对象。</summary>
    FontType GetFont();

    /// <summary>获取字间距（毫米）。</summary>
    double? GetLetterSpacing();

    /// <summary>获取文字字号（毫米）。</summary>
    double? GetFontSize();
}
