namespace Ofd.Core.BasicStructure.PageTree;

/// <summary>
/// 页树（图 12）：页顺序为页树前序遍历时叶节点的顺序。
/// </summary>
public class Pages : OFDElement
{
    public Pages(Dom.DomElement proxy) : base(proxy) { }

    public Pages() : base("Pages") { }

    /// <summary>【必选】增加 叶节点。</summary>
    public Pages AddPage(Page page)
    {
        Add(page);
        return this;
    }

    /// <summary>获取页面数量。</summary>
    public int GetSize() => Elements().Count;

    /// <summary>【必选】获取 叶节点序列（≥1）。</summary>
    public List<Page> GetPages() => GetOFDElements("Page", e => new Page(e));

    /// <summary>获取指定页面（index 从 0 起）。</summary>
    public Page GetPageByIndex(int index) => GetPages()[index];
}
