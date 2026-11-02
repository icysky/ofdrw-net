using System.Globalization;
using Ofd.Core.BasicType;

namespace Ofd.Core.PageDescription.Color.Color;

/// <summary>
/// 轴向渐变（8.3.4.2 图 29/30 表 29）：颜色沿起始点与结束点决定的轴线渐变，
/// 垂直于轴线的直线上颜色相同。
/// </summary>
public class CT_AxialShd : OFDElement, IColorClusterType
{
    public CT_AxialShd(Dom.DomElement proxy) : base(proxy) { }

    public CT_AxialShd() : base("AxialShd") { }

    /// <summary>【可选属性】设置 渐变绘制的方式（null 删除属性，缺省 Direct）。</summary>
    public CT_AxialShd SetMapType(MapType? mapType)
    {
        if (mapType == null)
        {
            RemoveAttr("MapType");
            return this;
        }
        return (CT_AxialShd)AddAttribute("MapType", mapType.ToString());
    }

    /// <summary>【可选属性】获取 渐变绘制的方式，缺省 Direct。</summary>
    public MapType GetMapType() => MapTypeExtensions.GetInstance(AttributeValue("MapType"));

    /// <summary>【可选属性】设置 轴线一个渐变区间的长度（null 删除属性，缺省轴线长度）。</summary>
    public CT_AxialShd SetMapUnit(double? mapUnit)
    {
        if (mapUnit == null)
        {
            RemoveAttr("MapUnit");
            return this;
        }
        return (CT_AxialShd)AddAttribute("MapUnit", STBase.Fmt(mapUnit.Value));
    }

    /// <summary>【可选属性】获取 轴线一个渐变区间的长度，不存在 null。</summary>
    public double? GetMapUnit()
    {
        var str = AttributeValue("MapUnit");
        if (str == null || str.Trim().Length == 0) return null;
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>【可选属性】设置 轴线延长线方向是否继续绘制（null 删除属性，缺省 _0）。</summary>
    public CT_AxialShd SetExtend(Extend? extend)
    {
        if (extend == null)
        {
            RemoveAttr("Extend");
            return this;
        }
        return (CT_AxialShd)AddAttribute("Extend", extend.Value.ToXmlValue());
    }

    /// <summary>【可选属性】获取 轴线延长线方向是否继续绘制，缺省 _0。</summary>
    public Extend GetExtend() => ExtendExtensions.GetInstance(AttributeValue("Extend"));

    /// <summary>【必选属性】设置 轴线起始点。</summary>
    public CT_AxialShd SetStartPoint(ST_Pos startPoint) => (CT_AxialShd)AddAttribute("StartPoint", startPoint.ToString());

    /// <summary>【必选属性】获取 轴线起始点，不存在 null。</summary>
    public ST_Pos? GetStartPoint() => ST_Pos.GetInstance(AttributeValue("StartPoint"));

    /// <summary>【必选属性】设置 轴线结束点。</summary>
    public CT_AxialShd SetEndPoint(ST_Pos endPoint) => (CT_AxialShd)AddAttribute("EndPoint", endPoint.ToString());

    /// <summary>【必选属性】获取 轴线结束点，不存在 null。</summary>
    public ST_Pos? GetEndPoint() => ST_Pos.GetInstance(AttributeValue("EndPoint"));

    /// <summary>【必选】增加 段（null 抛异常）。</summary>
    public CT_AxialShd AddSegment(Segment segment) => (CT_AxialShd)Add(segment);

    /// <summary>【必选】获取 段列表。</summary>
    public List<Segment> GetSegments() => GetOFDElements("Segment", e => new Segment(e));
}
