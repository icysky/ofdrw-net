using Ofd.Core.Text.Text;
using Ofd.Core.Text.Font;

namespace Ofd.Reader;

/// <summary>关键字检索命中的文本资源（页码 + 字体 + 文字对象）。</summary>
public class KeywordResource
{
    /// <summary>页码。</summary>
    private int _page;

    /// <summary>字体对象。</summary>
    private CT_Font? _font;

    /// <summary>文字对象。</summary>
    private CT_Text? _text;

    public int GetPage() => _page;

    public void SetPage(int page) => _page = page;

    public CT_Font? GetFont() => _font;

    public void SetFont(CT_Font? font) => _font = font;

    public CT_Text? GetText() => _text;

    public void SetText(CT_Text? text) => _text = text;
}
