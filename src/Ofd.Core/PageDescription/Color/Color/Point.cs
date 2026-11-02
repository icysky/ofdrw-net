using System.Globalization;

namespace Ofd.Core.PageDescription.Color.Color;

/// <summary>渐变控制点（8.6.4.4 表 31 附录 A.13），至少出现三个。</summary>
public class Point : OFDElement
{
    public Point(Dom.DomElement proxy) : base(proxy) { }

    public Point() : base("Point") { }

    /// <summary>【必选属性】设置 控制点水平位置。</summary>
    public Point SetX(double x) => (Point)AddAttribute("X", x.ToString(CultureInfo.InvariantCulture));

    /// <summary>【必选属性】获取 控制点水平位置，缺失抛异常（与 Java 一致）。</summary>
    public double GetX() => GetRequired("X", "控制点水平位置（X）为空");

    /// <summary>【必选属性】设置 控制点垂直位置。</summary>
    public Point SetY(double y) => (Point)AddAttribute("Y", y.ToString(CultureInfo.InvariantCulture));

    /// <summary>【必选属性】获取 控制点垂直位置，缺失抛异常。</summary>
    public double GetY() => GetRequired("Y", "控制点垂直位置（Y）为空");

    /// <summary>【可选属性】设置 三角单元切换的方向标志（null 删除属性）。</summary>
    public Point SetEdgeFlag(EdgeFlag? edgeFlag)
    {
        if (edgeFlag == null)
        {
            RemoveAttr("EdgeFlag");
            return this;
        }
        return (Point)AddAttribute("EdgeFlag", edgeFlag.Value.ToXmlValue());
    }

    /// <summary>【可选属性】获取 三角单元切换的方向标志，缺省 _0。</summary>
    public EdgeFlag GetEdgeFlag() => EdgeFlagExtensions.GetInstance(AttributeValue("EdgeFlag"));

    /// <summary>【必选】设置 控制点对应的颜色（应为基本颜色；null 抛异常）。</summary>
    public Point SetColor(CT_Color color)
    {
        Set(color);
        return this;
    }

    /// <summary>【必选】获取 控制点对应的颜色，不存在 null。</summary>
    public CT_Color? GetColor()
    {
        var e = GetOFDElement("Color");
        return e == null ? null : new CT_Color(e);
    }

    private double GetRequired(string name, string errorMsg)
    {
        var str = AttributeValue(name);
        if (str == null || str.Trim().Length == 0) throw new InvalidOperationException(errorMsg);
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
