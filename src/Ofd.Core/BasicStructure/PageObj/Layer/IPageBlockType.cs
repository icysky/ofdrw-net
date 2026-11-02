using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.Dom;

namespace Ofd.Core.BasicStructure.PageObj.Layer;

/// <summary>页块类型标记接口（Java PageBlockType，逻辑层面表示）。</summary>
public interface IPageBlockType
{
}

/// <summary>
/// 页块类型工厂（Java PageBlockType.getInstance 的静态方法外置）：
/// 按元素名分派到 TextObject/PathObject/ImageObject/CompositeObject/PageBlock/Layer，未知返回 null。
/// </summary>
public static class PageBlockTypeFactory
{
    public static IPageBlockType? GetInstance(DomElement element)
    {
        string qName = element.Prefix.Length > 0 ? element.Prefix + ":" + element.Name : element.Name;
        return qName switch
        {
            "ofd:TextObject" or "TextObject" => new TextObject(element),
            "ofd:PathObject" or "PathObject" => new PathObject(element),
            "ofd:ImageObject" or "ImageObject" => new ImageObject(element),
            "ofd:CompositeObject" or "CompositeObject" => new CompositeObject(element),
            "ofd:PageBlock" or "PageBlock" => new CT_PageBlock(element),
            "ofd:Layer" or "Layer" => new CT_Layer(element),
            _ => null,
        };
    }
}
