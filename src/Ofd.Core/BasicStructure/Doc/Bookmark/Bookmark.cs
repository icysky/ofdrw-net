using Ofd.Core.Action.ActionType.ActionGoto;

namespace Ofd.Core.BasicStructure.Doc.Bookmark;

/// <summary>书签：将常用位置定义为书签，文档可包含一组书签（7.5 图 11）。</summary>
public class Bookmark : OFDElement
{
    public Bookmark(Dom.DomElement proxy) : base(proxy) { }

    public Bookmark() : base("Bookmark") { }

    /// <summary>创建书签。</summary>
    public Bookmark(string name, CT_Dest dest) : this()
    {
        SetBookmarkName(name).SetDest(dest);
    }

    /// <summary>【必选属性】设置 书签名称。</summary>
    public Bookmark SetBookmarkName(string name) => (Bookmark)AddAttribute("Name", name);

    /// <summary>【必选属性】获取 书签名称，不存在 null。</summary>
    public string? GetBookmarkName() => AttributeValue("Name");

    /// <summary>【必选】设置 书签对应的文档版位置。</summary>
    public Bookmark SetDest(CT_Dest dest)
    {
        Set(dest);
        return this;
    }

    /// <summary>【必选】获取 书签对应的文档版位置，不存在 null。</summary>
    public CT_Dest? GetDest()
    {
        var e = GetOFDElement("Dest");
        return e == null ? null : new CT_Dest(e);
    }
}
