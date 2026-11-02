using Ofd.Core.BasicType;

namespace Ofd.Core.PageDescription.Color.ColorSpace;

/// <summary>调色板中预定义的颜色（8.3 表 25），索引从 0 开始。</summary>
public class CV : OFDElement
{
    public CV(Dom.DomElement proxy) : base(proxy) { }

    public CV() : base("CV") { }

    public CV(ST_Array color) : this()
    {
        SetColor(color);
    }

    /// <summary>设置 预定义的颜色（addText 追加语义，与 Java 一致）。</summary>
    public CV SetColor(ST_Array value)
    {
        Proxy.Kids.Add(new Dom.DomText(value.ToString()));
        return this;
    }

    /// <summary>获取 预定义的颜色，不存在 null。</summary>
    public ST_Array? GetColor() => ST_Array.GetInstance(GetText());
}
