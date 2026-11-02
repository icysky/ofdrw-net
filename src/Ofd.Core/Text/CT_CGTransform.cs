using System.Globalization;
using Ofd.Core.BasicType;

namespace Ofd.Core.Text;

/// <summary>变换描述（11.4.1 图 66 表 48）：字符编码与字形索引的对应关系。</summary>
public class CT_CGTransform : OFDElement
{
    public CT_CGTransform(Dom.DomElement proxy) : base(proxy) { }

    public CT_CGTransform() : base("CGTransform") { }

    /// <summary>【必选属性】设置 字符编码起始位置（从 0 开始）。</summary>
    public CT_CGTransform SetCodePosition(int codePosition) =>
        (CT_CGTransform)AddAttribute("CodePosition", codePosition.ToString(CultureInfo.InvariantCulture));

    /// <summary>【必选属性】获取 字符编码起始位置，缺失抛异常（与 Java 一致）。</summary>
    public int GetCodePosition()
    {
        var str = AttributeValue("CodePosition");
        if (str == null || str.Trim().Length == 0)
            throw new InvalidOperationException("字符编码的起始位置不能为空");
        return int.Parse(str.Trim(), CultureInfo.InvariantCulture);
    }

    /// <summary>【可选属性】设置 变换关系中字符数量（≥1，null 按 1）。</summary>
    public CT_CGTransform SetCodeCount(int? codeCount)
    {
        int v = codeCount ?? 1;
        if (v < 1) throw new ArgumentException("变换关系中字符的数值应大于等于 1");
        return (CT_CGTransform)AddAttribute("CodeCount", v.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>【可选属性】获取 字符数量，缺省 1。</summary>
    public int GetCodeCount() => GetIntDefault("CodeCount", 1);

    /// <summary>【可选属性】设置 变换关系中字形索引个数（≥1，null 按 1）。</summary>
    public CT_CGTransform SetGlyphCount(int? glyphCount)
    {
        int v = glyphCount ?? 1;
        if (v < 1) throw new ArgumentException("变换关系中字符的数值应大于等于 1");
        return (CT_CGTransform)AddAttribute("GlyphCount", v.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>【可选属性】获取 字形索引个数，缺省 1。</summary>
    public int GetGlyphCount() => GetIntDefault("GlyphCount", 1);

    /// <summary>【可选】设置 变换后的字形索引列表。</summary>
    public CT_CGTransform SetGlyphs(ST_Array glyphs) => (CT_CGTransform)SetOFDEntity("Glyphs", glyphs.ToString());

    /// <summary>【可选】获取 变换后的字形索引列表，不存在 null。</summary>
    public ST_Array? GetGlyphs() => ST_Array.GetInstance(GetOFDElementText("Glyphs"));

    private int GetIntDefault(string name, int defaultValue)
    {
        var str = AttributeValue(name);
        if (str == null || str.Trim().Length == 0) return defaultValue;
        return int.Parse(str.Trim(), CultureInfo.InvariantCulture);
    }
}
