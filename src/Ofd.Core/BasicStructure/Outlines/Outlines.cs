using System.Collections;

namespace Ofd.Core.BasicStructure.Outlines;

/// <summary>大纲（图 18 大纲节点结构），树形组织。</summary>
public class Outlines : OFDElement, IEnumerable<CT_OutlineElem>
{
    public Outlines(Dom.DomElement proxy) : base(proxy) { }

    public Outlines() : base("Outlines") { }

    /// <summary>【必选】增加 大纲节点。</summary>
    public Outlines AddOutlineElem(CT_OutlineElem outlineElem)
    {
        Add(outlineElem);
        return this;
    }

    /// <summary>【必选】获取 大纲节点列表。</summary>
    public List<CT_OutlineElem> GetOutlineElems() => GetOFDElements("OutlineElem", e => new CT_OutlineElem(e));

    public IEnumerator<CT_OutlineElem> GetEnumerator() => GetOutlineElems().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
