using Ofd.Core.Action;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicType;

namespace Ofd.Core.BasicStructure.PageObj;

/// <summary>
/// 页对象（7.7 图 13 表 12）：支持模板页描述，Area/Content/Actions/PageRes 均可选。
/// 注：与 pageTree.Page（页树节点）不同类，本类为页面描述内容。
/// </summary>
public class PageObj : OFDElement
{
    public PageObj(Dom.DomElement proxy) : base(proxy) { }

    public PageObj() : base("Page") { }

    /// <summary>【可选】设置 页面区域（仅对本页有效，元素名为 Area）。</summary>
    public PageObj SetArea(CT_PageArea area)
    {
        area.SetOFDName("Area");
        Set(area);
        return this;
    }

    /// <summary>【可选】获取 页面区域，不存在 null。</summary>
    public CT_PageArea? GetArea()
    {
        var e = GetOFDElement("Area");
        return e == null ? null : new CT_PageArea(e);
    }

    /// <summary>【可选】增加 页面使用的模板页（null 忽略）。</summary>
    public PageObj AddTemplate(Template? template)
    {
        if (template == null) return this;
        Add(template);
        return this;
    }

    /// <summary>【可选】获取 页面使用的模板页列表。</summary>
    public List<Template> GetTemplates() => GetOFDElements("Template", e => new Template(e));

    /// <summary>【可选】增加 页资源路径引用。</summary>
    public PageObj AddPageRes(ST_Loc pageRes)
    {
        AddOFDEntity("PageRes", pageRes);
        return this;
    }

    /// <summary>【可选】获取 页资源路径列表。</summary>
    public List<ST_Loc?> GetPageResList() => GetOFDElements("PageRes", ElementText).Select(ST_Loc.GetInstance).ToList();

    /// <summary>【可选】设置 页面内容描述（不存在时表示空白页）。</summary>
    public PageObj SetContent(Content content)
    {
        Set(content);
        return this;
    }

    /// <summary>【可选】获取 页面内容描述，不存在 null。</summary>
    public Content? GetContent()
    {
        var e = GetOFDElement("Content");
        return e == null ? null : new Content(e);
    }

    /// <summary>【可选】设置 与页面关联的动作序列（事件类型 PO）。</summary>
    public PageObj SetActions(Actions actions)
    {
        Set(actions);
        return this;
    }

    /// <summary>【可选】获取 与页面关联的动作序列，不存在 null。</summary>
    public Actions? GetActions()
    {
        var e = GetOFDElement("Actions");
        return e == null ? null : new Actions(e);
    }
}
