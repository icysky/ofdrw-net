using Ofd.Core.Annotation.PageAnnot;

namespace Ofd.Core.Annotation;

/// <summary>
/// 注释入口文件（15.1 图 80 表 60）：注释按页面组织索引，注释内容在分页注释文件中描述。
/// </summary>
public class Annotations : OFDElement
{
    public Annotations(Dom.DomElement proxy) : base(proxy) { }

    public Annotations() : base("Annotations") { }

    /// <summary>【可选】增加 注释所在页（null 忽略）。</summary>
    public Annotations AddPage(AnnPage? page)
    {
        if (page == null) return this;
        Add(page);
        return this;
    }

    /// <summary>根据页面 ID 获取注释所在页，不存在 null。</summary>
    public AnnPage? GetByPageId(string id)
    {
        foreach (var page in GetPages())
            if (page.GetPageID()?.ToString() == id)
                return page;
        return null;
    }

    /// <summary>【可选】获取 注释所在页序列。</summary>
    public List<AnnPage> GetPages() => GetOFDElements("Page", e => new AnnPage(e));
}
