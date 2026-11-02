using Ofd.Core.BasicType;

namespace Ofd.Core.BasicStructure.PageTree;

/// <summary>页节点（7.6 表 11 页树属性）。</summary>
public class Page : OFDElement
{
    public Page(Dom.DomElement proxy) : base(proxy) { }

    public Page() : base("Page") { }

    /// <summary>创建页节点。</summary>
    public Page(long id, string baseLoc) : this()
    {
        SetID(new ST_ID(id)).SetBaseLoc(ST_Loc.GetInstance(baseLoc));
    }

    public Page(ST_ID id, ST_Loc? baseLoc) : this()
    {
        SetID(id).SetBaseLoc(baseLoc);
    }

    /// <summary>【必选属性】设置 页的标识符，不能与已有标识重复。</summary>
    public Page SetID(ST_ID id) => (Page)AddAttribute("ID", id.ToString());

    /// <summary>【必选属性】获取 页的标识符，不存在 null。</summary>
    public ST_ID? GetID() => ST_ID.GetInstance(AttributeValue("ID"));

    /// <summary>【必选属性】设置 页对象描述文件路径（null 删除属性）。</summary>
    public Page SetBaseLoc(ST_Loc? baseLoc)
    {
        if (baseLoc == null)
        {
            RemoveAttr("BaseLoc");
            return this;
        }
        return (Page)AddAttribute("BaseLoc", baseLoc.ToString());
    }

    /// <summary>【必选属性】获取 页对象描述文件路径，不存在 null。</summary>
    public ST_Loc? GetBaseLoc() => ST_Loc.GetInstance(AttributeValue("BaseLoc"));
}
