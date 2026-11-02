using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicType;
using LayerType = Ofd.Core.BasicStructure.PageObj.Layer.Type;

namespace Ofd.Core.BasicStructure.PageObj;

/// <summary>模板页（图 14）：内容结构与普通页相同。</summary>
public class CT_TemplatePage : OFDElement
{
    public CT_TemplatePage(Dom.DomElement proxy) : base(proxy) { }

    public CT_TemplatePage() : base("TemplatePage") { }

    /// <summary>【必选属性】设置 模板页的标识符，不能与已有标识符重复。</summary>
    public CT_TemplatePage SetID(ST_ID id) => (CT_TemplatePage)AddAttribute("ID", id.ToString());

    /// <summary>【必选属性】获取 模板页的标识符，不存在 null。</summary>
    public ST_ID? GetID() => ST_ID.GetInstance(AttributeValue("ID"));

    /// <summary>【可选属性】设置 模板页名称（空白删除属性）。</summary>
    public CT_TemplatePage SetTemplatePageName(string? name)
    {
        if (name == null || name.Trim().Length == 0)
        {
            RemoveAttr("Name");
            return this;
        }
        return (CT_TemplatePage)AddAttribute("Name", name);
    }

    /// <summary>
    /// 【可选属性】获取 模板页名称。
    /// 注：Java 上游返回类型误写为 ST_ID（Name 是字符串），C# 侧修正为 string。
    /// </summary>
    public string? GetTemplatePageName() => AttributeValue("Name");

    /// <summary>【可选属性】设置 模板页的默认视图类型（默认 Background；null 删除属性）。</summary>
    public CT_TemplatePage SetZOrder(LayerType? zOrder)
    {
        if (zOrder == null)
        {
            RemoveAttr("ZOrder");
            return this;
        }
        return (CT_TemplatePage)AddAttribute("ZOrder", zOrder.ToString());
    }

    /// <summary>【可选属性】获取 模板页的默认视图类型，缺省 Background。</summary>
    public LayerType GetZOrder()
    {
        var value = AttributeValue("ZOrder");
        return value == null || value.Trim().Length == 0 ? LayerType.Background : TypeExtensions.GetInstance(value);
    }

    /// <summary>【可选属性】设置 指向模板页内容描述文件路径（null 删除属性）。</summary>
    public CT_TemplatePage SetBaseLoc(ST_Loc? baseLoc)
    {
        if (baseLoc == null)
        {
            RemoveAttr("BaseLoc");
            return this;
        }
        return (CT_TemplatePage)AddAttribute("BaseLoc", baseLoc.ToString());
    }

    /// <summary>【可选属性】获取 指向模板页内容描述文件路径，不存在 null。</summary>
    public ST_Loc? GetBaseLoc() => ST_Loc.GetInstance(AttributeValue("BaseLoc"));
}
