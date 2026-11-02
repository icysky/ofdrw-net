using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicType;
using LayerType = Ofd.Core.BasicStructure.PageObj.Layer.Type;

namespace Ofd.Reader.Model;

/// <summary>页面模板对象实体（Java TemplatePageEntity）。</summary>
public class TemplatePageEntity
{
    /// <summary>模板内容。</summary>
    private readonly PageObj _page;

    /// <summary>模板信息。</summary>
    private readonly CT_TemplatePage? _tplInfo;

    /// <summary>页面顺序。</summary>
    private LayerType _order;

    /// <summary>由模板信息与模板页面内容创建实例。</summary>
    public TemplatePageEntity(CT_TemplatePage tplInfo, PageObj page)
    {
        _page = page;
        _tplInfo = tplInfo;
        _order = tplInfo.GetZOrder();
    }

    /// <summary>用于构造排序的实例（仅顺序与页面内容）。</summary>
    public TemplatePageEntity(LayerType order, PageObj page)
    {
        _page = page;
        _order = order;
    }

    /// <summary>获取模板 ID，无模板信息时返回 null。</summary>
    public string? GetID() => _tplInfo?.GetID()?.ToString();

    /// <summary>获取模板页名称，可能为 null。</summary>
    public string? GetTemplatePageName() => _tplInfo?.GetTemplatePageName();

    /// <summary>获取模板页的默认视图类型。</summary>
    public LayerType GetZOrder() => _order;

    /// <summary>通过页面配置的 ZOrder 覆盖 commonData 配置的顺序（null 忽略）。</summary>
    public void SetOrder(LayerType? order)
    {
        if (order == null) return;
        _order = order.Value;
    }

    /// <summary>获取指向模板页内容描述文件的路径，无模板信息时返回 null。</summary>
    public ST_Loc? GetBaseLoc() => _tplInfo?.GetBaseLoc();

    /// <summary>获取模板页面内容。</summary>
    public PageObj GetPage() => _page;

    /// <summary>获取模板对象信息。</summary>
    public CT_TemplatePage? GetTplInfo() => _tplInfo;
}
