using Ofd.Core.PageDescription.Color.Color;

namespace Ofd.Core.Image;

/// <summary>边框颜色（10 表 43）：CT_Color 以 BorderColor 元素名承载。</summary>
public class BorderColor : CT_Color
{
    public BorderColor(Dom.DomElement proxy) : base(proxy) { }

    public BorderColor() : base("BorderColor") { }
}
