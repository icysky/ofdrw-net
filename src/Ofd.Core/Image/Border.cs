using System.Globalization;
using Ofd.Core.BasicType;
using Ofd.Core.PageDescription.Color.Color;

namespace Ofd.Core.Image;

/// <summary>图像边框（第 10 章 表 43）。</summary>
public class Border : OFDElement
{
    public Border(Dom.DomElement proxy) : base(proxy) { }

    public Border() : base("Border") { }

    /// <summary>【可选属性】设置 边框线宽（0 表示不绘制；null 删除属性，缺省 0.353mm）。</summary>
    public Border SetLineWidth(double? lineWidth)
    {
        if (lineWidth == null)
        {
            RemoveAttr("LineWidth");
            return this;
        }
        return (Border)AddAttribute("LineWidth", STBase.Fmt(lineWidth.Value));
    }

    /// <summary>【可选属性】获取 边框线宽，缺省 0.353mm。</summary>
    public double GetLineWidth() => GetDouble("LineWidth", 0.353d);

    /// <summary>【可选属性】设置 边框水平角半径（null 删除属性，缺省 0）。</summary>
    public Border SetHorizonalCornerRadius(double? horizonalCornerRadius)
    {
        if (horizonalCornerRadius == null)
        {
            RemoveAttr("HorizonalCornerRadius");
            return this;
        }
        return (Border)AddAttribute("HorizonalCornerRadius", STBase.Fmt(horizonalCornerRadius.Value));
    }

    /// <summary>【可选属性】获取 边框水平角半径，缺省 0。</summary>
    public double GetHorizonalCornerRadius() => GetDouble("HorizonalCornerRadius", 0d);

    /// <summary>【可选属性】设置 边框垂直角半径（null 删除属性，缺省 0）。</summary>
    public Border SetVerticalCornerRadius(double? verticalCornerRadius)
    {
        if (verticalCornerRadius == null)
        {
            RemoveAttr("VerticalCornerRadius");
            return this;
        }
        return (Border)AddAttribute("VerticalCornerRadius", STBase.Fmt(verticalCornerRadius.Value));
    }

    /// <summary>【可选属性】获取 边框垂直角半径，缺省 0。</summary>
    public double GetVerticalCornerRadius() => GetDouble("VerticalCornerRadius", 0d);

    /// <summary>【可选属性】设置 边框虚线开始位置（null 删除属性，缺省 0）。</summary>
    public Border SetDashOffset(double? dashOffset)
    {
        if (dashOffset == null)
        {
            RemoveAttr("DashOffset");
            return this;
        }
        return (Border)AddAttribute("DashOffset", STBase.Fmt(dashOffset.Value));
    }

    /// <summary>【可选属性】获取 边框虚线开始位置，缺省 0。</summary>
    public double GetDashOffset() => GetDouble("DashOffset", 0d);

    /// <summary>【可选属性】设置 边框虚线重复样式（null 删除属性）。</summary>
    public Border SetDashPattern(ST_Array? dashPattern)
    {
        if (dashPattern == null)
        {
            RemoveAttr("DashPattern");
            return this;
        }
        return (Border)AddAttribute("DashPattern", dashPattern.ToString());
    }

    /// <summary>【可选属性】获取 边框虚线重复样式，不存在 null。</summary>
    public ST_Array? GetDashPattern() => ST_Array.GetInstance(AttributeValue("DashPattern"));

    /// <summary>
    /// 【可选】设置 边框颜色（null 忽略）。
    /// 注：Java 上游误将 Border 自身改名为 BorderColor，C# 修正为改传入颜色元素名。
    /// </summary>
    public Border SetBorderColor(CT_Color? borderColor)
    {
        if (borderColor == null) return this;
        borderColor.SetOFDName("BorderColor");
        Add(borderColor);
        return this;
    }

    /// <summary>【可选】获取 边框颜色，null 表示黑色。</summary>
    public BorderColor? GetBorderColor()
    {
        var e = GetOFDElement("BorderColor");
        return e == null ? null : new BorderColor(e);
    }

    private double GetDouble(string name, double defaultValue)
    {
        var str = AttributeValue(name);
        if (str == null || str.Trim().Length == 0) return defaultValue;
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
