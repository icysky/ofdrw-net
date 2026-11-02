using Ofd.Core.BasicStructure.Res.Resources;
using Ofd.Core.Dom;

namespace Ofd.Core.BasicStructure.Res;

/// <summary>
/// 资源文件抽象类型（Java OFDResource 接口）：绘制参数、颜色空间、字形、图像、音视频等。
/// </summary>
public interface IOFDResource
{
}

/// <summary>
/// 资源类型工厂（Java OFDResource.getInstance(Element) 的静态方法外置）：
/// 按元素限定名分派到具体资源容器；未知名称按包含关键字兜底。
/// </summary>
public static class OFDResourceFactory
{
    public static IOFDResource GetInstance(DomElement element)
    {
        string qName = element.Prefix.Length > 0 ? element.Prefix + ":" + element.Name : element.Name;
        switch (qName)
        {
            case "ofd:ColorSpaces":
            case "ColorSpaces":
                return new ColorSpaces(element);
            case "ofd:DrawParams":
            case "DrawParams":
                return new DrawParams(element);
            case "ofd:Fonts":
            case "Fonts":
                return new Fonts(element);
            case "ofd:MultiMedias":
            case "MultiMedias":
                return new MultiMedias(element);
            case "ofd:CompositeGraphicUnits":
            case "CompositeGraphicUnits":
                return new global::Ofd.Core.CompositeObj.CompositeGraphicUnits(element);
            default:
                var lower = qName.ToLowerInvariant();
                if (lower.Contains("draw")) return new DrawParams(element);
                if (lower.Contains("font")) return new Fonts(element);
                if (lower.Contains("color")) return new ColorSpaces(element);
                throw new ArgumentException("不是 Res 的子类，未知元素类型：" + qName);
        }
    }
}
