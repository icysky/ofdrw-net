using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicType;
using Ofd.Core.CompositeObj;

namespace Ofd.Core.BasicStructure.PageObj.Layer.Block;

/// <summary>复合对象（7.7 表 16，内容见第 13 章）。</summary>
public class CompositeObject : CT_Composite, IPageBlockType
{
    public CompositeObject(Dom.DomElement proxy) : base(proxy) { }

    private CompositeObject() : base("CompositeObject") { }

    public CompositeObject(ST_ID id) : this()
    {
        SetObjID(id);
    }

    /// <summary>【必选属性】设置 对象 ID（null 抛异常）。</summary>
    public CompositeObject SetId(ST_ID id)
    {
        if (id == null) throw new ArgumentException("ID 不能为空");
        SetObjID(id);
        return this;
    }

    /// <summary>【必选属性】获取 对象 ID，不存在 null。</summary>
    public ST_ID? GetID() => GetObjID();

    /// <summary>【必选属性】设置 引用资源文件中定义的矢量图像标识（null 抛异常）。</summary>
    public CompositeObject SetResourceID(ST_RefID resourceId)
    {
        if (resourceId == null) throw new ArgumentException("资源文件的标识（ResourceID）不能为空");
        return (CompositeObject)AddAttribute("ResourceID", resourceId.ToString());
    }

    /// <summary>【必选属性】获取 引用矢量图像标识，不存在 null。</summary>
    public ST_RefID? GetResourceID() => ST_RefID.GetInstance(AttributeValue("ResourceID"));
}
