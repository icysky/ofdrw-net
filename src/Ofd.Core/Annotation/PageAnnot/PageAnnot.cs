namespace Ofd.Core.Annotation.PageAnnot;

/// <summary>分页注释文件（15.2 图 81 表 61）。</summary>
public class PageAnnot : OFDElement
{
    public PageAnnot(Dom.DomElement proxy) : base(proxy) { }

    public PageAnnot() : base("PageAnnot") { }

    /// <summary>【必选】增加 注释对象（null 忽略）。</summary>
    public PageAnnot AddAnnot(Annot? annot)
    {
        if (annot == null) return this;
        Add(annot);
        return this;
    }

    /// <summary>【必选】获取 注释对象列表。</summary>
    public List<Annot> GetAnnots() => GetOFDElements("Annot", e => new Annot(e));
}
