using System.Globalization;
using Ofd.Core.BasicType;

namespace Ofd.Core.PageDescription.Color.Color;

/// <summary>颜色段（8.3.4.2 表 29），至少出现两个。</summary>
public class Segment : OFDElement
{
    public Segment(Dom.DomElement proxy) : base(proxy) { }

    public Segment() : base("Segment") { }

    public Segment(CT_Color color) : this()
    {
        SetColor(color);
    }

    public Segment(double position, CT_Color color) : this()
    {
        SetColor(color).SetPosition(position);
    }

    /// <summary>
    /// 【可选属性】设置 渐变段颜色位置参数 [0, 1.0]（null 删除属性；
    /// 缺省时首段 0、末段 1.0，中间空缺区间平局分配）。
    /// </summary>
    public Segment SetPosition(double? position)
    {
        if (position == null)
        {
            RemoveAttr("Position");
            return this;
        }
        return (Segment)AddAttribute("Position", STBase.Fmt(position.Value));
    }

    /// <summary>【可选属性】获取 渐变段颜色位置参数，null 表示不存在。</summary>
    public double? GetPosition()
    {
        var str = AttributeValue("Position");
        if (str == null || str.Trim().Length == 0) return null;
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>【必选】设置 该段的颜色（应为基本颜色；null 抛异常）。</summary>
    public Segment SetColor(CT_Color color)
    {
        RemoveAll();
        Add(color);
        return this;
    }

    /// <summary>【必选】获取 该段的颜色，不存在 null。</summary>
    public CT_Color? GetColor()
    {
        var e = GetOFDElement("Color");
        return e == null ? null : new CT_Color(e);
    }
}
