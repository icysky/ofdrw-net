using System.Globalization;
using Ofd.Core.BasicType;

namespace Ofd.Core.PageDescription.Color.Color;

/// <summary>
/// 基本颜色（8.3.2 图 25 表 26）：通道值（Value）或调色板索引（Index）两种指定方式，
/// 也可引用颜色空间（ColorSpace）、设置透明度（Alpha）。
/// </summary>
public class CT_Color : OFDElement
{
    public CT_Color(Dom.DomElement proxy) : base(proxy) { }

    public CT_Color() : base("Color") { }

    /// <summary>供 FillColor/StrokeColor 等子类指定元素名。</summary>
    protected CT_Color(string name) : base(name) { }

    /// <summary>构造 RGB 颜色（10 进制通道值，0~255）。</summary>
    public static CT_Color Rgb(int r, int g, int b) => new CT_Color().SetValue(new ST_Array(r, g, b));

    public static CT_Color Rgb(int[] rgb) => Rgb(rgb[0], rgb[1], rgb[2]);

    /// <summary>【可选属性】设置 颜色值（"通道1 通道2 ..."，通道可为 "#" 十六进制；null 删属性）。</summary>
    public CT_Color SetValue(ST_Array? value)
    {
        if (value == null)
        {
            RemoveAttr("Value");
            return this;
        }
        return (CT_Color)AddAttribute("Value", value.ToString());
    }

    /// <summary>【可选属性】获取 颜色值，不存在 null。</summary>
    public ST_Array? GetValue() => ST_Array.GetInstance(AttributeValue("Value"));

    /// <summary>【可选属性】设置 调色板中颜色的编号（非负；null 删属性）。</summary>
    public CT_Color SetIndex(int? index)
    {
        if (index == null)
        {
            RemoveAttr("Index");
            return this;
        }
        if (index < 0)
            throw new ArgumentException("调色板中颜色的编号，必须为非负整数");
        return (CT_Color)AddAttribute("Index", index.Value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>【可选属性】获取 调色板中颜色的编号，null 表示不存在；负值抛异常。</summary>
    public int? GetIndex()
    {
        var str = AttributeValue("Index");
        if (str == null || str.Trim().Length == 0) return null;
        int index = int.Parse(str.Trim(), CultureInfo.InvariantCulture);
        if (index < 0)
            throw new ArgumentException("调色板中颜色的编号，必须为非负整数");
        return index;
    }

    /// <summary>【可选属性】设置 引用资源文件中颜色空间的标识（null 删属性）。</summary>
    public CT_Color SetColorSpace(ST_RefID? colorSpace)
    {
        if (colorSpace == null)
        {
            RemoveAttr("ColorSpace");
            return this;
        }
        return (CT_Color)AddAttribute("ColorSpace", colorSpace.ToString());
    }

    /// <summary>【可选属性】获取 引用资源文件中颜色空间的标识，null 表示使用文档默认颜色空间。</summary>
    public ST_RefID? GetColorSpace() => ST_RefID.GetInstance(AttributeValue("ColorSpace"));

    /// <summary>【可选属性】设置 颜色透明度（0~255 钳制；null 删属性）。</summary>
    public CT_Color SetAlpha(int? alpha)
    {
        if (alpha == null)
        {
            RemoveAttr("Alpha");
            return this;
        }
        int v = alpha.Value;
        if (v < 0) v = 0;
        else if (v > 255) v = 255;
        return (CT_Color)AddAttribute("Alpha", v.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>【可选属性】获取 颜色透明度，缺省 255；越界抛异常。</summary>
    public int GetAlpha()
    {
        var str = AttributeValue("Alpha");
        if (str == null || str.Trim().Length == 0) return 255;
        int value = int.Parse(str.Trim(), CultureInfo.InvariantCulture);
        if (value < 0 || value > 255)
            throw new ArgumentException("颜色透明度范围在 0~255 之间取值：" + value);
        return value;
    }

    /// <summary>【可选】设置 颜色族（底纹/渐变；先清除已有颜色族元素，null 忽略）。</summary>
    public CT_Color SetColor(IColorClusterType? color)
    {
        if (color == null) return this;
        RemoveOFDElemByNames("Pattern", "AxialShd", "RadialShd", "GouraudShd", "LaGouraudShd");
        Add((OFDElement)color);
        return this;
    }

    /// <summary>【可选】获取 颜色族（底纹/渐变），null 表示不存在。</summary>
    public IColorClusterType? GetColor()
    {
        var elements = Elements();
        if (elements.Count == 0) return null;
        return ColorClusterTypeFactory.GetInstance(elements[0]);
    }

    /// <summary>【可选】获取 指定类型的颜色族，类型不符/解析失败返回 null。</summary>
    public T? GetColorByType<T>() where T : class, IColorClusterType
    {
        var elements = Elements();
        if (elements.Count == 0) return null;
        try
        {
            return ColorClusterTypeFactory.GetInstance(elements[0]) as T;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
