namespace Ofd.Core.Graph.Tight;

/// <summary>区域（图 49）：由一系列闭合分路径（Area）组成，用于动作启动区域等。</summary>
public class CT_Region : OFDElement
{
    public CT_Region(Dom.DomElement proxy) : base(proxy) { }

    public CT_Region() : base("Region") { }

    /// <summary>【必选】为区域增加分路径（null 忽略）。</summary>
    public CT_Region AddArea(CT_Area? area)
    {
        if (area == null) return this;
        Add(area);
        return this;
    }

    /// <summary>【必选】获取 区域中所有分路径。</summary>
    public List<CT_Area> GetAreas() => GetOFDElements("Area", e => new CT_Area(e));
}
