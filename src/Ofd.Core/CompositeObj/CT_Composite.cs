using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Core.PageDescription;

namespace Ofd.Core.CompositeObj;

/// <summary>
/// 复合对象（第 13 章 图 71 表 49）：内容在 ResourceID 指向的矢量图像资源（CT_VectorG）中描述，
/// 一个资源可被多个复合对象引用。
/// </summary>
public class CT_Composite : CT_GraphicUnit<CT_Composite>
{
    public CT_Composite(Dom.DomElement proxy) : base(proxy) { }

    public CT_Composite() : base("Composite") { }

    protected CT_Composite(string name) : base(name) { }

    /// <summary>构造复合对象（改名 CompositeObject 并设置 ID）。</summary>
    public CompositeObject ToObj(ST_ID id)
    {
        SetOFDName("CompositeObject");
        SetObjID(id);
        return new CompositeObject(Proxy);
    }

    /// <summary>【必选属性】设置 引用资源文件中定义的矢量图像标识。</summary>
    public CT_Composite SetResourceID(ST_RefID resourceId) => (CT_Composite)AddAttribute("ResourceID", resourceId.ToString());

    /// <summary>【必选属性】获取 引用矢量图像标识，不存在 null。</summary>
    public ST_RefID? GetResourceID() => ST_RefID.GetInstance(AttributeValue("ResourceID"));
}
