using Ofd.Core.BasicStructure.Res.Resources;
using Ofd.Core.BasicType;

namespace Ofd.Core.BasicStructure.Res;

/// <summary>
/// 资源（7.9 图 20）：绘制图元所需数据（绘制参数、颜色空间、字形、图像、音视频等）的集合。
/// 一个文档可能包含一个或多个资源文件；资源按作用范围分为公共资源和页资源。
/// </summary>
public class Res : OFDElement
{
    public Res(Dom.DomElement proxy) : base(proxy) { }

    public Res() : base("Res") { }

    /// <summary>【必选属性】设置 此资源文件的通用数据存储路径。</summary>
    public Res SetBaseLoc(ST_Loc baseLoc) => (Res)AddAttribute("BaseLoc", baseLoc.ToString());

    /// <summary>【必选属性】获取 通用数据存储路径，不存在 null。</summary>
    public ST_Loc? GetBaseLoc() => ST_Loc.GetInstance(AttributeValue("BaseLoc"));

    /// <summary>【可选】添加 资源（0~多个）。</summary>
    public Res AddResource(IOFDResource resource)
    {
        Add((OFDElement)resource);
        return this;
    }

    /// <summary>获取字体资源容器（Fonts）列表。</summary>
    public List<Fonts> GetFonts() => GetResources().OfType<Fonts>().ToList();

    /// <summary>【可选】获取 资源列表（按元素名工厂分派）。</summary>
    public List<IOFDResource> GetResources() => Elements().Select(OFDResourceFactory.GetInstance).ToList();
}
