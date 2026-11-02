using Ofd.Font;
using FontType = Ofd.Font.Font;

namespace Ofd.Layout.Element;

/// <summary>文字字符块（Java TxtGlyph）：单个字符的宽高计算（含字间距）。</summary>
public class TxtGlyph
{
    /// <summary>文字本身大小（延迟计算并缓存）。</summary>
    private TextBounds? _bounds;

    /// <summary>文字。</summary>
    private readonly char _txt;

    /// <summary>文字字体信息。</summary>
    private readonly ITextFontInfo _textFontInfo;

    public TxtGlyph(char txt, ITextFontInfo span)
    {
        _txt = txt;
        _textFontInfo = span;
    }

    /// <summary>字符宽度（字符宽度 + 字间距）。</summary>
    public double GetW()
    {
        FontType font = _textFontInfo.GetFont();
        double letterSpacing = _textFontInfo.GetLetterSpacing() ?? 0d;
        double fontSize = _textFontInfo.GetFontSize() ?? 0d;

        if (font.HasWidthMath())
            return letterSpacing + fontSize * font.GetCharWidthScale(_txt);

        _bounds ??= EnvFont.StrBounds(font.GetName(), font.GetFamilyName(), _txt.ToString(), fontSize);
        return letterSpacing + _bounds.Value.Width;
    }

    /// <summary>字符高度。</summary>
    public double GetH()
    {
        FontType font = _textFontInfo.GetFont();
        double fontSize = _textFontInfo.GetFontSize() ?? 0d;

        if (font.HasWidthMath()) return fontSize;

        _bounds ??= EnvFont.StrBounds(font.GetName(), font.GetFamilyName(), _txt.ToString(), fontSize);
        return _bounds.Value.Height;
    }
}
