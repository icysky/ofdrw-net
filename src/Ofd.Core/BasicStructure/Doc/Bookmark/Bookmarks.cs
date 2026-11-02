namespace Ofd.Core.BasicStructure.Doc.Bookmark;

/// <summary>文档的书签集（7.5 表 5 文档根节点属性），包含一组书签。</summary>
public class Bookmarks : OFDElement
{
    public Bookmarks(Dom.DomElement proxy) : base(proxy) { }

    public Bookmarks() : base("Bookmarks") { }

    /// <summary>增加 书签。</summary>
    public Bookmarks AddBookmark(Bookmark bookmark)
    {
        Add(bookmark);
        return this;
    }

    /// <summary>获取 书签列表。</summary>
    public List<Bookmark> GetBookmarks() => GetOFDElements("Bookmark", e => new Bookmark(e));
}
