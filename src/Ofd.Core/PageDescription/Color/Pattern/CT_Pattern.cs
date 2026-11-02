using System.Globalization;
using Ofd.Core.BasicType;
using Ofd.Core.PageDescription.Color.Color;

namespace Ofd.Core.PageDescription.Color.Pattern;

/// <summary>底纹（8.3.3 图 26 表 28）：用于图形和文字的填充以及勾边处理。</summary>
public class CT_Pattern : OFDElement, IColorClusterType
{
    public CT_Pattern(Dom.DomElement proxy) : base(proxy) { }

    public CT_Pattern() : base("Pattern") { }

    /// <summary>【必选属性】设置 底纹单元宽度。</summary>
    public CT_Pattern SetWidth(double width) => (CT_Pattern)AddAttribute("Width", STBase.Fmt(width));

    /// <summary>【必选属性】获取 底纹单元宽度，缺失抛异常（与 Java 一致）。</summary>
    public double GetWidth() => GetRequiredDouble("Width", "底纹格式非法，底纹单元宽度不能为空");

    /// <summary>【必选属性】设置 底纹单元高度。</summary>
    public CT_Pattern SetHeight(double height) => (CT_Pattern)AddAttribute("Height", STBase.Fmt(height));

    /// <summary>【必选属性】获取 底纹单元高度，缺失抛异常。</summary>
    public double GetHeight() => GetRequiredDouble("Height", "底纹格式非法，底纹单元高度不能为空");

    /// <summary>
    /// 【可选属性】设置 X 方向底纹单元间距（小于宽度时按宽度处理；null 删除属性）。
    /// 注：Width 未设置时抛异常（依赖 GetWidth，与 Java 一致）。
    /// </summary>
    public CT_Pattern SetXStep(double? xStep)
    {
        if (xStep == null)
        {
            RemoveAttr("XStep");
            return this;
        }
        double width = GetWidth();
        if (xStep < width) xStep = width;
        return (CT_Pattern)AddAttribute("XStep", STBase.Fmt(xStep.Value));
    }

    /// <summary>【可选属性】获取 X 方向底纹单元间距（缺省/小于宽度时返回宽度）。</summary>
    public double GetXStep()
    {
        var str = AttributeValue("XStep");
        if (str == null || str.Trim().Length == 0) return GetWidth();
        double var = double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
        double width = GetWidth();
        return var < width ? width : var;
    }

    /// <summary>【可选属性】设置 Y 方向底纹单元间距（小于高度时按高度处理；null 删除属性）。</summary>
    public CT_Pattern SetYStep(double? yStep)
    {
        if (yStep == null)
        {
            RemoveAttr("YStep");
            return this;
        }
        double height = GetHeight();
        if (yStep < height) yStep = height;
        return (CT_Pattern)AddAttribute("YStep", STBase.Fmt(yStep.Value));
    }

    /// <summary>【可选属性】获取 Y 方向底纹单元间距（缺省/小于高度时返回高度）。</summary>
    public double GetYStep()
    {
        var str = AttributeValue("YStep");
        if (str == null || str.Trim().Length == 0) return GetHeight();
        double var = double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
        double height = GetHeight();
        return var < height ? height : var;
    }

    /// <summary>【可选属性】设置 底纹单元翻转方式（null 删除属性，缺省 Normal）。</summary>
    public CT_Pattern SetReflectMethod(ReflectMethod? reflectMethod)
    {
        if (reflectMethod == null)
        {
            RemoveAttr("ReflectMethod");
            return this;
        }
        return (CT_Pattern)AddAttribute("ReflectMethod", reflectMethod.ToString());
    }

    /// <summary>【可选属性】获取 底纹单元翻转方式，缺省 Normal。</summary>
    public ReflectMethod GetReflectMethod() => ReflectMethodExtensions.GetInstance(AttributeValue("ReflectMethod"));

    /// <summary>【可选属性】设置 底纹单元起始位置（null 删除属性，缺省 Object）。</summary>
    public CT_Pattern SetRelativeTo(RelativeTo? relativeTo)
    {
        if (relativeTo == null)
        {
            RemoveAttr("RelativeTo");
            return this;
        }
        return (CT_Pattern)AddAttribute("RelativeTo", relativeTo.ToString());
    }

    /// <summary>【可选属性】获取 底纹单元起始位置，缺省 Object。</summary>
    public RelativeTo GetRelativeTo() => RelativeToExtensions.GetInstance(AttributeValue("RelativeTo"));

    /// <summary>【可选属性】设置 底纹单元变换矩阵（null 删除属性，缺省单位矩阵）。</summary>
    public CT_Pattern SetCTM(ST_Array? ctm)
    {
        if (ctm == null)
        {
            RemoveAttr("CTM");
            return this;
        }
        return (CT_Pattern)AddAttribute("CTM", ctm.ToString());
    }

    /// <summary>【可选属性】获取 底纹单元变换矩阵，不存在 null。</summary>
    public ST_Array? GetCTM() => ST_Array.GetInstance(AttributeValue("CTM"));

    /// <summary>【必选】设置 底纹单元。</summary>
    public CT_Pattern SetCellContent(CellContent cellContent)
    {
        Set(cellContent);
        return this;
    }

    /// <summary>【必选】获取 底纹单元，缺失抛异常（与 Java 一致）。</summary>
    public CellContent GetCellContent()
    {
        var e = GetOFDElement("CellContent");
        if (e == null) throw new InvalidOperationException("底纹格式异常，未找到任何可用底纹单元（CellContent）");
        return new CellContent(e);
    }

    private double GetRequiredDouble(string name, string errorMsg)
    {
        var str = AttributeValue(name);
        if (str == null || str.Trim().Length == 0) throw new InvalidOperationException(errorMsg);
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
