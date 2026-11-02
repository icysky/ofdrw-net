using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicType;
using Ofd.Core.Text.Text;

namespace Ofd.Core.BasicStructure.PageObj.Layer.Block;

/// <summary>文字对象（7.7 表 16，内容见 11.2）。</summary>
public class TextObject : CT_Text, IPageBlockType
{
    public TextObject(Dom.DomElement proxy) : base(proxy) { }

    private TextObject() : base("TextObject") { }

    public TextObject(ST_ID id) : this()
    {
        SetObjID(id);
    }

    public TextObject(long id) : this()
    {
        SetObjID(new ST_ID(id));
    }

    /// <summary>【必选属性】设置 对象 ID（null 抛异常）。</summary>
    public TextObject SetId(ST_ID id)
    {
        if (id == null) throw new ArgumentException("ID 不能为空");
        SetObjID(id);
        return this;
    }

    /// <summary>【必选属性】获取 对象 ID，不存在 null。</summary>
    public ST_ID? GetID() => GetObjID();
}
