using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicType;
using LayerType = Ofd.Core.BasicStructure.PageObj.Layer.Type;

namespace Ofd.Core.BasicStructure.PageObj;

/// <summary>
/// 页面使用的模板页（7.7）：通过 TemplateID 引用 CommonData 中的模板，
/// ZOrder 控制呈现顺序（默认 Background）；在模板页的内容描述中无效。
/// </summary>
public class Template : OFDElement
{
    public Template(Dom.DomElement proxy) : base(proxy) { }

    public Template() : base("Template") { }

    /// <summary>【必选属性】设置 引用 CommonData 中定义的模板标识符。</summary>
    public Template SetTemplateID(ST_RefID templateId) => (Template)AddAttribute("TemplateID", templateId.ToString());

    /// <summary>【必选属性】获取 模板标识符引用，不存在 null。</summary>
    public ST_RefID? GetTemplateID() => ST_RefID.GetInstance(AttributeValue("TemplateID"));

    /// <summary>【可选属性】设置 模板呈现顺序（null 删除属性，缺省 Background）。</summary>
    public Template SetZOrder(LayerType? zOrder)
    {
        if (zOrder == null)
        {
            RemoveAttr("ZOrder");
            return this;
        }
        return (Template)AddAttribute("ZOrder", zOrder.ToString());
    }

    /// <summary>【可选属性】获取 模板呈现顺序，缺省 Background（Type.GetInstance 对空值返回 Background）。</summary>
    public LayerType GetZOrder() => TypeExtensions.GetInstance(AttributeValue("ZOrder"));
}
