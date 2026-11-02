using Ofd.Core.Annotation.PageAnnot;

namespace Ofd.Reader.Model;

/// <summary>注释实体（Java AnnotionEntity）。</summary>
public class AnnotionEntity
{
    private readonly string _pageId;
    private readonly List<Annot> _annots;

    public AnnotionEntity(string pageId, List<Annot> annots)
    {
        _pageId = pageId;
        _annots = annots;
    }

    /// <summary>注释所在页面 ID。</summary>
    public string GetPageId() => _pageId;

    /// <summary>注释列表。</summary>
    public List<Annot> GetAnnots() => _annots;
}
