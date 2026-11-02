using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicType;
using Ofd.Core.Graph.PathObj;

namespace Ofd.Core.BasicStructure.PageObj.Layer.Block;

/// <summary>图形对象（7.7 表 16，内容见 9.1）。</summary>
public class PathObject : CT_Path, IPageBlockType
{
    public PathObject(Dom.DomElement proxy) : base(proxy) { }

    private PathObject() : base("PathObject") { }

    public PathObject(ST_ID id) : this()
    {
        SetObjID(id);
    }

    /// <summary>【必选属性】设置 对象 ID（null 抛异常）。</summary>
    public PathObject SetId(ST_ID id)
    {
        if (id == null) throw new ArgumentException("ID 不能为空");
        SetObjID(id);
        return this;
    }

    public PathObject SetId(long id) => SetId(new ST_ID(id));

    /// <summary>【必选属性】获取 对象 ID，不存在 null。</summary>
    public ST_ID? GetID() => GetObjID();
}
