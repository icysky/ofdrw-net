using Ofd.Core.Dom;
using Ofd.Core.PageDescription.Color.Pattern;

namespace Ofd.Core.PageDescription.Color.Color;

/// <summary>颜色族标记接口（Java ColorClusterType）：基本颜色、底纹和渐变。</summary>
public interface IColorClusterType
{
}

/// <summary>颜色族工厂（Java ColorClusterType.getInstance 的静态方法外置）。</summary>
public static class ColorClusterTypeFactory
{
    public static IColorClusterType GetInstance(DomElement element)
    {
        string qName = element.Prefix.Length > 0 ? element.Prefix + ":" + element.Name : element.Name;
        return qName switch
        {
            "ofd:Pattern" => new CT_Pattern(element),
            "ofd:AxialShd" => new CT_AxialShd(element),
            "ofd:RadialShd" => new CT_RadialShd(element),
            "ofd:GouraudShd" => new CT_GouraudShd(element),
            "ofd:LaGouraudShd" => new CT_LaGouraudShd(element),
            _ => throw new ArgumentException("未知的元素类型不是 颜色子类：" + qName),
        };
    }
}
