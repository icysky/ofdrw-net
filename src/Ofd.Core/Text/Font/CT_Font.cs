using Ofd.Core.BasicType;

namespace Ofd.Core.Text.Font;

/// <summary>字形（11.1 图 58 表 44）。</summary>
public class CT_Font : OFDElement
{
    public CT_Font(Dom.DomElement proxy) : base(proxy) { }

    public CT_Font() : base("Font") { }

    /// <summary>以字形名创建。</summary>
    public CT_Font(string fontName) : this()
    {
        SetFontName(fontName);
    }

    public ST_ID? GetID() => GetObjID();

    public CT_Font SetID(ST_ID id) => (CT_Font)SetObjID(id);

    public CT_Font SetID(long id) => (CT_Font)SetObjID(new ST_ID(id));

    /// <summary>【必选属性】设置 字形名。</summary>
    public CT_Font SetFontName(string fontName) => (CT_Font)AddAttribute("FontName", fontName);

    /// <summary>【必选属性】获取 字形名，缺失抛异常（与 Java 一致）。</summary>
    public string GetFontName()
    {
        var str = AttributeValue("FontName");
        if (str == null || str.Trim().Length == 0)
            throw new ArgumentException("字形名（fontName）不能为空");
        return str;
    }

    /// <summary>【可选属性】设置 字形族名（空白忽略不设置）。</summary>
    public CT_Font SetFamilyName(string? familyName)
    {
        if (familyName == null || familyName.Trim().Length == 0) return this;
        return (CT_Font)AddAttribute("FamilyName", familyName);
    }

    /// <summary>【可选属性】获取 字形族名，不存在 null。</summary>
    public string? GetFamilyName() => AttributeValue("FamilyName");

    /// <summary>【可选属性】设置 字符分类（null 按默认 unicode）。</summary>
    public CT_Font SetCharset(Charset? charset) => (CT_Font)AddAttribute("Charset", (charset ?? Charset.unicode).ToXmlValue());

    /// <summary>【可选属性】获取 字符分类，缺失/空默认 unicode。</summary>
    public Charset GetCharset() => CharsetExtensions.GetInstance(AttributeValue("Charset"));

    /// <summary>【可选属性】设置 是否斜体（null 按 false）。</summary>
    public CT_Font SetItalic(bool? italic) => (CT_Font)AddAttribute("Italic", (italic ?? false) ? "true" : "false");

    /// <summary>【可选属性】获取 是否斜体，缺省 false。</summary>
    public bool GetItalic() => BoolAttr("Italic");

    /// <summary>【可选属性】设置 是否粗体（null 按 false）。</summary>
    public CT_Font SetBold(bool? bold) => (CT_Font)AddAttribute("Bold", (bold ?? false) ? "true" : "false");

    /// <summary>【可选属性】获取 是否粗体，缺省 false。</summary>
    public bool GetBold() => BoolAttr("Bold");

    /// <summary>【可选属性】设置 是否带衬线（null 按 false）。</summary>
    public CT_Font SetSerif(bool? serif) => (CT_Font)AddAttribute("Serif", (serif ?? false) ? "true" : "false");

    /// <summary>【可选属性】获取 是否带衬线，缺省 false。</summary>
    public bool GetSerif() => BoolAttr("Serif");

    /// <summary>【可选属性】设置 是否等宽（null 按 false）。</summary>
    public CT_Font SetFixedWidth(bool? fixedWidth) => (CT_Font)AddAttribute("FixedWidth", (fixedWidth ?? false) ? "true" : "false");

    /// <summary>【可选属性】获取 是否等宽，缺省 false。</summary>
    public bool GetFixedWidth() => BoolAttr("FixedWidth");

    /// <summary>【可选】设置 指向内嵌字形文件（OpenType 格式）。</summary>
    public CT_Font SetFontFile(ST_Loc fontFile) => (CT_Font)SetOFDEntity("FontFile", fontFile);

    public CT_Font SetFontFile(string path) => SetFontFile(new ST_Loc(path));

    /// <summary>【可选】获取 指向内嵌字形文件，不存在 null。</summary>
    public ST_Loc? GetFontFile() => ST_Loc.GetInstance(GetOFDElementText("FontFile"));

    private bool BoolAttr(string name)
    {
        var str = AttributeValue(name);
        return str != null && str.Trim().Length != 0 && bool.Parse(str.Trim());
    }
}
