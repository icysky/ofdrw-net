using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using LayerType = Ofd.Core.BasicStructure.PageObj.Layer.Type;

namespace Ofd.Core.BasicStructure.PageObj.Layer;

/// <summary>页图层（7.7）：层类型描述 + 图元内容。</summary>
public class CT_Layer : CT_PageBlock
{
    public CT_Layer(Dom.DomElement proxy) : base(proxy) { }

    public CT_Layer() : base("Layer") { }

    /// <summary>【可选属性】设置 层类型描述（null 删除属性，缺省 Body）。</summary>
    public CT_Layer SetType(LayerType? type)
    {
        if (type == null)
        {
            RemoveAttr("Type");
            return this;
        }
        return (CT_Layer)AddAttribute("Type", type.ToString());
    }

    /// <summary>【可选属性】获取 层类型描述，缺省 Body。</summary>
    public LayerType GetType() => TypeExtensions.GetInstance(AttributeValue("Type"));

    /// <summary>【可选属性】设置 图层绘制参数引用（null 删除属性）。</summary>
    public CT_Layer SetDrawParam(ST_RefID? drawParam)
    {
        if (drawParam == null)
        {
            RemoveAttr("DrawParam");
            return this;
        }
        return (CT_Layer)AddAttribute("DrawParam", drawParam.ToString());
    }

    /// <summary>【可选属性】获取 图层绘制参数引用，不存在 null。</summary>
    public ST_RefID? GetDrawParam() => ST_RefID.GetInstance(AttributeValue("DrawParam"));
}
