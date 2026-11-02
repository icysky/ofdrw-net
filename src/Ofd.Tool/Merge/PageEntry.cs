namespace Ofd.Tool.Merge;

/// <summary>
/// 合并时的页面条目（Java PageEntry）：指定文档的某一页，可附带需叠加到该页上的其他文档页面。
/// </summary>
public class PageEntry
{
    /// <summary>关联文档上下文。</summary>
    public DocContext DocCtx;

    /// <summary>页面索引号（从 1 开始）。</summary>
    public int? PageIndex;

    /// <summary>是否复制模板（默认 true）。</summary>
    public bool CopyTemplate = true;

    /// <summary>是否复制注释（默认 true）。</summary>
    public bool CopyAnnotations = true;

    /// <summary>需要混合到指定页面之上的其他文档页面。</summary>
    public List<PageEntry>? TbMixPages;

    /// <summary>创建迁移页面对象。</summary>
    public PageEntry(int? pageIndex, DocContext docCtx)
    {
        DocCtx = docCtx;
        PageIndex = pageIndex;
    }

    /// <summary>创建迁移页面对象（附带叠加页面）。</summary>
    public PageEntry(int? pageIndex, DocContext docCtx, params PageEntry[] tbMixPages) : this(pageIndex, docCtx)
    {
        if (tbMixPages != null && tbMixPages.Length > 0) TbMixPages = tbMixPages.ToList();
    }
}
