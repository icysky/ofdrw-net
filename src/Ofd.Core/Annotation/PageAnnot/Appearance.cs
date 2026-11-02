using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;

namespace Ofd.Core.Annotation.PageAnnot;

/// <summary>注释的静态呈现效果（15.2 图 81 表 61）：使用页面块定义描述，含必选边界。</summary>
public class Appearance : CT_PageBlock
{
    public Appearance(Dom.DomElement proxy) : base(proxy) { }

    public Appearance(ST_Box boundary) : base("Appearance")
    {
        SetBoundary(boundary);
    }

    /// <summary>【必选】设置 边界（null 抛异常）。</summary>
    public Appearance SetBoundary(ST_Box boundary)
    {
        if (boundary == null) throw new ArgumentException("Boundary 不能为空");
        return (Appearance)AddAttribute("Boundary", boundary.ToString());
    }

    /// <summary>【必选】获取 边界，不存在 null。</summary>
    public ST_Box? GetBoundary() => ST_Box.GetInstance(AttributeValue("Boundary"));

    /// <summary>【可选】增加 页块（返回 Appearance 支持链式）。</summary>
    public new Appearance AddPageBlock(IPageBlockType pageBlock)
    {
        base.AddPageBlock(pageBlock);
        return this;
    }
}
