namespace Ofd.Core.BasicStructure.Outlines;

/// <summary>大纲节点（图 19），可嵌套形成树。</summary>
public class CT_OutlineElem : OFDElement
{
    public CT_OutlineElem(Dom.DomElement proxy) : base(proxy) { }

    public CT_OutlineElem() : base("OutlineElem") { }

    public CT_OutlineElem(string title) : this()
    {
        SetTitle(title);
    }

    /// <summary>【必选属性】设置 大纲节点标题。</summary>
    public CT_OutlineElem SetTitle(string title) => (CT_OutlineElem)AddAttribute("Title", title);

    /// <summary>【必选属性】获取 大纲节点标题，不存在 null。</summary>
    public string? GetTitle() => AttributeValue("Title");

    /// <summary>【可选属性】设置 该节点下所有叶节点的数目参考值（默认 0）。</summary>
    public CT_OutlineElem SetCount(int count) =>
        (CT_OutlineElem)AddAttribute("Count", count.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>【可选属性】获取 该节点下所有叶节点的数目参考值，缺省 0。</summary>
    public int GetCount()
    {
        var str = AttributeValue("Count");
        if (str == null || str.Trim().Length == 0) return 0;
        return int.Parse(str.Trim(), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>【可选属性】设置 初始状态是否展开子节点（默认 true）。</summary>
    public CT_OutlineElem SetExpanded(bool expanded) => (CT_OutlineElem)AddAttribute("Expanded", expanded ? "true" : "false");

    /// <summary>【可选属性】获取 初始状态是否展开子节点，缺省 true。</summary>
    public bool GetExpanded()
    {
        var str = AttributeValue("Expanded");
        return str == null || str.Trim().Length == 0 || bool.Parse(str.Trim());
    }

    /// <summary>【可选】设置 当此大纲节点被激活时执行的动作序列。</summary>
    public CT_OutlineElem SetActions(global::Ofd.Core.Action.Actions actions)
    {
        Set(actions);
        return this;
    }

    /// <summary>【可选】获取 当此大纲节点被激活时执行的动作序列，不存在 null。</summary>
    public global::Ofd.Core.Action.Actions? GetActions()
    {
        var e = GetOFDElement("Actions");
        return e == null ? null : new global::Ofd.Core.Action.Actions(e);
    }

    /// <summary>【可选】增加 大纲子节点。</summary>
    public CT_OutlineElem AddOutlineElem(CT_OutlineElem outlineElem)
    {
        Add(outlineElem);
        return this;
    }

    /// <summary>【可选】获取 该大纲下所有子节点。</summary>
    public List<CT_OutlineElem> GetOutlineElems() => GetOFDElements("OutlineElem", e => new CT_OutlineElem(e));
}
