namespace Ofd.Core.Action.ActionType.ActionGoto;

/// <summary>
/// 跳转的目的书签（表 53 跳转动作属性）。
/// 注：Java 类名为 Bookmark，与 basicStructure.doc.bookmark.Bookmark 同名不同包；
/// C# 侧为避免 using 歧义命名为 GotoBookmark（元素名仍为 "Bookmark"，对照表记录映射）。
/// </summary>
public class GotoBookmark : OFDElement, IOFDGotoTarget
{
    public GotoBookmark(Dom.DomElement proxy) : base(proxy) { }

    public GotoBookmark() : base("Bookmark") { }

    public GotoBookmark(string name) : this()
    {
        SetBookmarkName(name);
    }

    /// <summary>【必选属性】设置 目标书签名称，引用文档书签中的名称。</summary>
    public GotoBookmark SetBookmarkName(string name) => (GotoBookmark)AddAttribute("Name", name);

    /// <summary>【必选属性】获取 目标书签名称，不存在 null。</summary>
    public string? GetBookmarkName() => AttributeValue("Name");
}
