using System.Globalization;
using Ofd.Core.BasicType;

namespace Ofd.Core.PageDescription.Color.Color;

/// <summary>
/// 径向渐变（8.3.4.3 图 35 表 30）：定义两个离心率与倾角相同的椭圆，
/// 在两椭圆边缘连线区域内渐变。
/// </summary>
public class CT_RadialShd : OFDElement, IColorClusterType
{
    public CT_RadialShd(Dom.DomElement proxy) : base(proxy) { }

    public CT_RadialShd() : base("RadialShd") { }

    /// <summary>【可选属性】设置 渐变绘制的方式（null 删除属性，缺省 Direct）。</summary>
    public CT_RadialShd SetMapType(MapType? mapType)
    {
        if (mapType == null)
        {
            RemoveAttr("MapType");
            return this;
        }
        return (CT_RadialShd)AddAttribute("MapType", mapType.ToString());
    }

    /// <summary>【可选属性】获取 渐变绘制的方式，缺省 Direct。</summary>
    public MapType GetMapType() => MapTypeExtensions.GetInstance(AttributeValue("MapType"));

    /// <summary>【可选属性】设置 轴线一个渐变区间的长度（null 删除属性）。</summary>
    public CT_RadialShd SetMapUnit(double? mapUnit)
    {
        if (mapUnit == null)
        {
            RemoveAttr("MapUnit");
            return this;
        }
        return (CT_RadialShd)AddAttribute("MapUnit", STBase.Fmt(mapUnit.Value));
    }

    /// <summary>【可选属性】获取 轴线一个渐变区间的长度，不存在 null。</summary>
    public double? GetMapUnit()
    {
        var str = AttributeValue("MapUnit");
        if (str == null || str.Trim().Length == 0) return null;
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>【可选属性】设置 两个椭圆的离心率 [0, 1.0)（null 删除属性，缺省 0 退化为圆）。</summary>
    public CT_RadialShd SetEccentricity(double? eccentricity)
    {
        if (eccentricity == null)
        {
            RemoveAttr("Eccentricity");
            return this;
        }
        return (CT_RadialShd)AddAttribute("Eccentricity", STBase.Fmt(eccentricity.Value));
    }

    /// <summary>【可选属性】获取 两个椭圆的离心率，缺省 0。</summary>
    public double GetEccentricity()
    {
        var str = AttributeValue("Eccentricity");
        if (str == null || str.Trim().Length == 0) return 0d;
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 【可选属性】设置 两个椭圆的倾斜角度（null 删除属性，缺省 0）。
    /// 注：Java 上游 null 分支未 return 导致后续 fmt(null) NPE，C# 已修正为直接返回。
    /// </summary>
    public CT_RadialShd SetAngle(double? angle)
    {
        if (angle == null)
        {
            RemoveAttr("Angle");
            return this;
        }
        return (CT_RadialShd)AddAttribute("Angle", STBase.Fmt(angle.Value));
    }

    /// <summary>【可选属性】获取 两个椭圆的倾斜角度，缺省 0。</summary>
    public double GetAngle()
    {
        var str = AttributeValue("Angle");
        if (str == null || str.Trim().Length == 0) return 0d;
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>【必选属性】设置 起始椭圆中心点。</summary>
    public CT_RadialShd SetStartPoint(ST_Pos startPoint) => (CT_RadialShd)AddAttribute("StartPoint", startPoint.ToString());

    /// <summary>【必选属性】获取 起始椭圆中心点，缺失抛异常（与 Java 一致）。</summary>
    public ST_Pos GetStartPoint()
    {
        var str = AttributeValue("StartPoint");
        if (str == null || str.Trim().Length == 0)
            throw new InvalidOperationException("起始椭圆的的中心点（startPoint）不能为空");
        return ST_Pos.GetInstance(str)!;
    }

    /// <summary>【必选属性】设置 结束椭圆中心点。</summary>
    public CT_RadialShd SetEndPoint(ST_Pos endPoint) => (CT_RadialShd)AddAttribute("EndPoint", endPoint.ToString());

    /// <summary>【必选属性】获取 结束椭圆中心点，缺失抛异常。</summary>
    public ST_Pos GetEndPoint()
    {
        var str = AttributeValue("EndPoint");
        if (str == null || str.Trim().Length == 0)
            throw new InvalidOperationException("结束椭圆的的中心点（EndPoint）不能为空");
        return ST_Pos.GetInstance(str)!;
    }

    /// <summary>【可选属性】设置 起始椭圆长半轴（null 删除属性，缺省 0）。</summary>
    public CT_RadialShd SetStartRadius(double? startRadius)
    {
        if (startRadius == null)
        {
            RemoveAttr("StartRadius");
            return this;
        }
        return (CT_RadialShd)AddAttribute("StartRadius", STBase.Fmt(startRadius.Value));
    }

    /// <summary>【可选属性】获取 起始椭圆长半轴，缺省 0。</summary>
    public double GetStartRadius()
    {
        var str = AttributeValue("StartRadius");
        if (str == null || str.Trim().Length == 0) return 0d;
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>【必选属性】设置 结束椭圆长半轴。</summary>
    public CT_RadialShd SetEndRadius(double endRadius) => (CT_RadialShd)AddAttribute("EndRadius", STBase.Fmt(endRadius));

    /// <summary>【必选属性】获取 结束椭圆长半轴，缺失抛异常。</summary>
    public double GetEndRadius()
    {
        var str = AttributeValue("EndRadius");
        if (str == null || str.Trim().Length == 0)
            throw new InvalidOperationException("结束椭圆的长半轴（EndRadius）不能为空");
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>【可选属性】设置 轴线延长线方向是否继续绘制（null 删除属性，缺省 _0）。</summary>
    public CT_RadialShd SetExtend(Extend? extend)
    {
        if (extend == null)
        {
            RemoveAttr("Extend");
            return this;
        }
        return (CT_RadialShd)AddAttribute("Extend", extend.Value.ToXmlValue());
    }

    /// <summary>【可选属性】获取 轴线延长线方向是否继续绘制，缺省 _0。</summary>
    public Extend GetExtend() => ExtendExtensions.GetInstance(AttributeValue("Extend"));

    /// <summary>【必选】增加 段（null 抛异常）。</summary>
    public CT_RadialShd AddSegment(Segment segment) => (CT_RadialShd)base.Add(segment);

    /// <summary>【必选】获取 段列表。</summary>
    public List<Segment> GetSegments() => GetOFDElements("Segment", e => new Segment(e));
}
