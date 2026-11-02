using Ofd.Core.PageDescription.Color.Color;

namespace Ofd.Core.Graph.PathObj;

/// <summary>勾边颜色（9.1 表 35）：CT_Color 以 StrokeColor 元素名承载。</summary>
public class StrokeColor : CT_Color
{
    public StrokeColor(Dom.DomElement proxy) : base(proxy) { }

    public StrokeColor() : base("StrokeColor") { }
}
