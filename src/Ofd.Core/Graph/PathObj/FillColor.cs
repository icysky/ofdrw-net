using Ofd.Core.PageDescription.Color.Color;

namespace Ofd.Core.Graph.PathObj;

/// <summary>填充颜色（9.1 表 35）：CT_Color 以 FillColor 元素名承载。</summary>
public class FillColor : CT_Color
{
    public FillColor(Dom.DomElement proxy) : base(proxy) { }

    public FillColor() : base("FillColor") { }
}
