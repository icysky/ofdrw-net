namespace Ofd.Core.PageDescription.Clips;

/// <summary>
/// 裁剪区（8.4 图 44 表 33）：由一组路径或文字构成，最终裁剪范围为各分路径并集，
/// 数据相对于所修饰图元对象的外接矩形。
/// </summary>
public class CT_Clip : OFDElement
{
    public CT_Clip(Dom.DomElement proxy) : base(proxy) { }

    public CT_Clip() : base("Clip") { }

    /// <summary>【必选】增加 裁剪区域（null 忽略）。</summary>
    public CT_Clip AddArea(Area? area)
    {
        if (area == null) return this;
        Add(area);
        return this;
    }

    /// <summary>【必选】获取 裁剪区域列表。</summary>
    public List<Area> GetAreas() => GetOFDElements("Area", e => new Area(e));
}
