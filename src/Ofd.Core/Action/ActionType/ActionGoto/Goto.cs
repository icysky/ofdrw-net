namespace Ofd.Core.Action.ActionType.ActionGoto;

/// <summary>跳转动作（图 74）：同文档内跳转到目的区域或书签位置。</summary>
public class Goto : OFDElement, IOFDAction
{
    public Goto(Dom.DomElement proxy) : base(proxy) { }

    public Goto() : base("Goto") { }

    public Goto(CT_Dest dest) : this()
    {
        SetDest(dest);
    }

    public Goto(string bookmarkName) : this()
    {
        SetBookmark(bookmarkName);
    }

    /// <summary>【必选】设置 跳转的目的区域（先移除已有 Bookmark/Dest）。</summary>
    public Goto SetDest(CT_Dest dest)
    {
        RemoveOFDElemByNames("Bookmark", "Dest");
        Add(dest);
        return this;
    }

    /// <summary>【必选】设置 跳转的目标书签（先移除已有 Bookmark/Dest）。</summary>
    public Goto SetBookmark(string name)
    {
        RemoveOFDElemByNames("Bookmark", "Dest");
        Add(new GotoBookmark(name));
        return this;
    }

    /// <summary>【必选】获取 跳转动作的目标（CT_Dest 或 Bookmark，可用类型判断）。</summary>
    public IOFDGotoTarget GetTarget()
    {
        var elements = Elements();
        if (elements.Count != 1)
            throw new InvalidOperationException("Goto 中含有多个目标位置无法确定");
        return OFDGotoTargetFactory.GetInstance(elements[0]);
    }
}
