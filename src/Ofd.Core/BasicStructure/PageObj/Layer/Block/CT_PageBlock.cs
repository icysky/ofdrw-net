using Ofd.Core.BasicStructure.PageObj.Layer;

namespace Ofd.Core.BasicStructure.PageObj.Layer.Block;

/// <summary>页块结构（7.7 图 17 表 16）：可以嵌套。</summary>
public class CT_PageBlock : OFDElement, IPageBlockType
{
    public CT_PageBlock(Dom.DomElement proxy) : base(proxy) { }

    public CT_PageBlock() : this("PageBlock") { }

    protected CT_PageBlock(string name) : base(name) { }

    /// <summary>【可选】增加 页块（可嵌套，可含 0~多个）。</summary>
    public CT_PageBlock AddPageBlock(IPageBlockType pageBlock)
    {
        Add((OFDElement)pageBlock);
        return this;
    }

    /// <summary>【可选】获取 当前页块内的所有页块（可用类型判断具体对象）。</summary>
    public List<IPageBlockType> GetPageBlocks()
    {
        var res = new List<IPageBlockType>();
        foreach (var element in Elements())
        {
            var pb = PageBlockTypeFactory.GetInstance(element);
            if (pb != null) res.Add(pb);
        }
        return res;
    }
}
