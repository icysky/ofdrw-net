using Ofd.Core.Text.Font;

namespace Ofd.Core.BasicStructure.Res.Resources;

/// <summary>包含了文档所有字形的描述（7.9 图 20 表 18）。</summary>
public class Fonts : OFDElement, IOFDResource
{
    public Fonts(Dom.DomElement proxy) : base(proxy) { }

    public Fonts() : base("Fonts") { }

    /// <summary>【必选】添加 字形描述（必须有 ID；null 忽略）。</summary>
    public Fonts AddFont(CT_Font? font)
    {
        if (font == null) return this;
        if (font.GetID() == null)
            throw new ArgumentException("字形描述ID不能为空");
        Add(font);
        return this;
    }

    /// <summary>【必选】获取 字形描述序列。</summary>
    public List<CT_Font> GetFonts() => GetOFDElements("Font", e => new CT_Font(e));
}
